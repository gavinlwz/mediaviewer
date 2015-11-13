﻿using VideoPlayerControl.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoLib;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Threading;

namespace VideoPlayerControl
{
   public class VideoPlayerViewModel : IDisposable
    {                
        public event EventHandler<bool> IsBufferingChanged;   
        public event EventHandler<VideoState> StateChanged;
        public event EventHandler<int> PositionSecondsChanged;
        public event EventHandler<int> DurationSecondsChanged;
        public event EventHandler<bool> HasAudioChanged;
             
        Control owner;

        public VideoLib.VideoPlayer.OutputPixelFormat DecodedVideoFormat {get; protected set;}
        public String VideoLocation { get; protected set; }
                    
        public bool DisplayOverlayText
        {
            get
            {
                return videoRender.DisplayInfoText;
            }
            set
            {
                videoRender.DisplayInfoText = value;
            }
        }


        int NrFramesRendered { get; set; }
        int NrFramesDropped { get; set; }
               
        VideoState videoState;

        public VideoState VideoState
        {
            get { return videoState; }
            set
            {
                videoState = value;

                if (StateChanged != null)
                {
                    StateChanged(this, value);
                }
            }
        }
              
        // no AV sync correction is done if below the AV sync threshold 
        const double AV_SYNC_THRESHOLD = 0.01;
        // no AV sync correction is done if too big error 
        const double AV_NOSYNC_THRESHOLD = 10.0;

        const double AUDIO_SAMPLE_CORRECTION_PERCENT_MAX = 10;

        // we use about AUDIO_DIFF_AVG_NB A-V differences to make the average 
        const int AUDIO_DIFF_AVG_NB = 5;//20;
              
        VideoLib.VideoPlayer videoDecoder;
        AudioPlayer audioPlayer;
        VideoRender videoRender;

        public int Width
        {
            get { return videoDecoder.Width; }
        }

        public int Height
        {
            get { return videoDecoder.Height; }

        }

        enum SyncMode
        {
            AUDIO_SYNCS_TO_VIDEO,
            VIDEO_SYNCS_TO_AUDIO
        };

        SyncMode syncMode;

        double previousVideoPts;
        double previousVideoDelay;

        double videoFrameTimer;
        double audioFrameTimer;

        HRTimer videoRefreshTimer;
        HRTimer audioRefreshTimer;

        double videoPts;
        double videoPtsDrift;

        double audioDiffCum;
        double audioDiffAvgCoef;
        double audioDiffThreshold;
        int audioDiffAvgCount;
   
        Task[] demuxPacketsTask;
        CancellationTokenSource demuxPacketsCancellationTokenSource;

        Task OpenTask { get; set; }
        CancellationTokenSource CancelTokenSource { get; set; }
    
        int positionSeconds;

        public int PositionSeconds
        {
            get { return positionSeconds; }
            set
            {
                positionSeconds = value;

                if (PositionSecondsChanged != null)
                {
                    PositionSecondsChanged(this, positionSeconds);
                }
            }
        }

        int durationSeconds;

        public int DurationSeconds
        {
            get { return durationSeconds; }
            set
            {
                durationSeconds = value;
                if (DurationSecondsChanged != null)
                {
                    DurationSecondsChanged(this, durationSeconds);
                }
            }
        }

        public bool IsMuted
        {
            get { return audioPlayer.IsMuted; }
            set
            {
                audioPlayer.IsMuted = value;
             
            }
        }

        bool hasAudio;

        public bool HasAudio {
            get
            {
                return (hasAudio);
            }
            set
            {
                hasAudio = value;

                if (HasAudioChanged != null)
                {
                    HasAudioChanged(this, hasAudio);
                }
            
            }
        }

        public double Volume
        {
            get { return audioPlayer.Volume; }
            set { audioPlayer.Volume = value; }
        }

        public int MaxVolume
        {
            get { return audioPlayer.MaxVolume; }           
        }
  
        public int MinVolume
        {
            get { return audioPlayer.MinVolume; }        
        }

        public double VideoClock { get; protected set; }
        public double AudioClock { get; protected set; }

        public void simulateLag(int i, bool isEnabled)
        {
            videoDecoder.IsSimulateLag[i] = isEnabled;
        }
              
        public log4net.ILog Log { get; set; }
             
        public VideoPlayerViewModel(Control owner,
            VideoLib.VideoPlayer.OutputPixelFormat decodedVideoFormat = VideoLib.VideoPlayer.OutputPixelFormat.YUV420P)
        {
       
            this.owner = owner;
            DecodedVideoFormat = decodedVideoFormat;            
       
            videoDecoder = new VideoLib.VideoPlayer();
            videoDecoder.setLogCallback(videoDecoderLogCallback, true, VideoLib.VideoPlayer.LogLevel.LOG_LEVEL_ERROR);

            videoDecoder.FrameQueue.Finished += new EventHandler((s,e) =>
            {
                owner.BeginInvoke(new Func<Task>(async () => await close()));                
            });

            videoDecoder.FrameQueue.IsBufferingChanged += new EventHandler((s, e) =>
                {
                    owner.BeginInvoke(new Action(() =>
                    {
                        if (IsBufferingChanged != null)
                        {
                            IsBufferingChanged(this, videoDecoder.FrameQueue.IsBuffering);
                        }
                    }));
                });
           
            audioPlayer = new AudioPlayer(owner);
            videoRender = new VideoRender(owner);

            audioDiffAvgCoef = Math.Exp(Math.Log(0.01) / AUDIO_DIFF_AVG_NB);

            //syncMode = SyncMode.AUDIO_SYNCS_TO_VIDEO;
            syncMode = SyncMode.VIDEO_SYNCS_TO_AUDIO;
            
            videoRefreshTimer = HRTimerFactory.create(HRTimerFactory.TimerType.TIMER_QUEUE);
            videoRefreshTimer.Tick += new EventHandler(videoRefreshTimer_Tick);
            videoRefreshTimer.AutoReset = false;

            audioRefreshTimer = HRTimerFactory.create(HRTimerFactory.TimerType.TIMER_QUEUE);
            audioRefreshTimer.Tick += new EventHandler(audioRefreshTimer_Tick);
            audioRefreshTimer.AutoReset = false;           

            DurationSeconds = 0;
            PositionSeconds = 0;

            owner.HandleDestroyed += new EventHandler(async (s, e) => await close());

            VideoState = VideoState.CLOSED;
            VideoLocation = "";

            //interruptIOTokenSource = new CancellationTokenSource();       
        }

        public void createScreenShot(String screenShotName, int positionOffset)
        {
            videoRender.createScreenShot(screenShotName, PositionSeconds, VideoLocation, positionOffset);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool safe)
        {
            if (safe)
            {
                if (videoRender != null)
                {
                    videoRender.Dispose();
                    videoRender = null;
                }
                if (audioPlayer != null)
                {
                    audioPlayer.Dispose();
                    audioPlayer = null;
                }
                if (videoDecoder != null)
                {
                    videoDecoder.Dispose();
                    videoDecoder = null;
                }
                /*if (demuxPacketsTask != null)
                {
                    demuxPacketsTask.Dispose();
                    demuxPacketsTask = null;
                }*/
                if (CancelTokenSource != null)
                {
                    CancelTokenSource.Dispose();
                    CancelTokenSource = null;
                }
                if (demuxPacketsCancellationTokenSource != null)
                {
                    demuxPacketsCancellationTokenSource.Dispose();
                    demuxPacketsCancellationTokenSource = null; 
                }
                if (videoRefreshTimer != null)
                {
                    videoRefreshTimer.Dispose();
                    videoRefreshTimer = null;
                }
                if (audioRefreshTimer != null)
                {
                    audioRefreshTimer.Dispose();
                    audioRefreshTimer = null;
                }
                
            }
        }

       

        void videoRefreshTimer_Tick(Object sender, EventArgs e)
        {

            bool skipVideoFrame = false;
            
restartvideo:
			
			double actualDelay = 0.04;

			// grab a decoded frame, returns null if the queue is paused or closed
			VideoFrame videoFrame = videoDecoder.FrameQueue.getDecodedVideoFrame();
            if (videoFrame == null)
            {
                if (VideoState == VideoState.CLOSED)
                {
                    videoRender.display(null, Color.Black, RenderMode.CLEAR_SCREEN);
                    videoRender.releaseResources();
                    return;
                }
                
                videoRender.display(null, Color.Black, RenderMode.PAUSED);		                
            }
            else
            {
                if (videoRender.RenderMode == RenderMode.PAUSED)
                {
                    // reset videoFrameTimer before (re)starting rendering
                    audioFrameTimer = videoFrameTimer = HRTimer.getTimestamp();
                }

                videoPts = videoFrame.Pts;
                videoPtsDrift = videoFrame.Pts + HRTimer.getTimestamp();

                if (skipVideoFrame == false)
                {
                    videoRender.display(videoFrame, Color.Black, RenderMode.NORMAL);
                }

                actualDelay = synchronizeVideo(videoPts);
       
                NrFramesRendered++;   
            }

            updateObservableVariables(); 

            if (actualDelay < 0.010)
            {
                // delay is too small skip next frame
                skipVideoFrame = true;
                NrFramesDropped++;
               
                goto restartvideo;

            }
                    
            // start timer with delay for next frame
            videoRefreshTimer.Interval = (int)(actualDelay * 1000 + 0.5);
            videoRefreshTimer.start();

        }

        void updateObservableVariables()
        {            
            VideoClock = getVideoClock();
            AudioClock = audioPlayer.getAudioClock();
            PositionSeconds = (int)Math.Floor(videoDecoder.HasAudio == true ? AudioClock : VideoClock);
                                              
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("State: " + VideoState.ToString());
            builder.Append("Free Packets (" + videoDecoder.FrameQueue.FreePacketQueueState.ToString() + ") ");                                            
            builder.AppendLine(": " + videoDecoder.FrameQueue.FreePacketsInQueue + "/" + videoDecoder.FrameQueue.MaxFreePackets);

            builder.Append("Video Packets (" + videoDecoder.FrameQueue.VideoPacketQueueState.ToString() + ") ");
            builder.AppendLine(": " + videoDecoder.FrameQueue.VideoPacketsInQueue + "/" + videoDecoder.FrameQueue.MaxVideoPackets);
                
            builder.Append("Audio Packets (" + videoDecoder.FrameQueue.AudioPacketQueueState.ToString() + ") ");               
            builder.AppendLine(": " + videoDecoder.FrameQueue.AudioPacketsInQueue + "/" + videoDecoder.FrameQueue.MaxAudioPackets);
            builder.AppendLine("Audio State: " + audioPlayer.Status.ToString());
            builder.AppendLine("Buffering: " + videoDecoder.FrameQueue.IsBuffering.ToString());
            builder.AppendLine("Video Packet Errors: " + videoDecoder.FrameQueue.NrVideoPacketReadErrors.ToString());               
            builder.AppendLine("Audio Packet Errors: " + videoDecoder.FrameQueue.NrAudioPacketReadErrors.ToString());               

            builder.AppendLine("Nr Frames Dropped: " + NrFramesDropped + "/" + NrFramesRendered);
            builder.AppendLine("Video Clock: " + VideoClock.ToString("#.####"));
            builder.AppendLine("Audio Clock: " + AudioClock.ToString("#.####"));

            videoRender.InfoText = builder.ToString();
            
        }
  
        double synchronizeVideo(double videoPts)
        {            
            // assume delay to next frame equals delay between previous frames
            double delay = videoPts - previousVideoPts;

            if (delay <= 0 || delay >= 1.0)
            {
                // if incorrect delay, use previous one 
                delay = previousVideoDelay;
            }

            previousVideoPts = videoPts;
            previousVideoDelay = delay;

            if (videoDecoder.HasAudio && syncMode == SyncMode.VIDEO_SYNCS_TO_AUDIO)
            {           
                // synchronize video to audio                                
                double diff = getVideoClock() - audioPlayer.getAudioClock();

                // Skip or repeat the frame. Take delay into account
                // FFPlay still doesn't "know if this is the best guess."
                double sync_threshold = (delay > AV_SYNC_THRESHOLD) ? delay : AV_SYNC_THRESHOLD;

                if (Math.Abs(diff) < AV_NOSYNC_THRESHOLD)
                {
                    if (diff <= -sync_threshold)
                    {
                        delay = 0;
                    }
                    else if (diff >= sync_threshold)
                    {
                        //delay = 2 * delay;
                        delay = diff;
                    }
                }

            }

            // adjust delay based on the actual current time
            videoFrameTimer += delay;
            double actualDelay = videoFrameTimer - HRTimer.getTimestamp();
         

            return (actualDelay);
        }

        void audioRefreshTimer_Tick(Object sender, EventArgs e)
        {

        restartaudio:

            double actualDelay = 0.04;

            if (!videoDecoder.HasVideo) updateObservableVariables(); 

            // returns null when framequeue is paused or closed
            VideoLib.AudioFrame audioFrame = videoDecoder.FrameQueue.getDecodedAudioFrame();
            if (audioFrame == null)
            {
                // stop audio if playing
                audioPlayer.stop();                

                if (VideoState == VideoState.CLOSED)
                {
                    audioPlayer.flush();
                    return;

                } 
                // when paused spin idle                                
            }
            else
            {                
                if (audioPlayer.Status == SharpDX.DirectSound.BufferStatus.None)
                {
                    // reset audio frame timer before (re)starting playing
                    audioFrameTimer = videoFrameTimer = HRTimer.getTimestamp();                  
                }

                // if the audio is lagging behind too much, skip the buffer completely
                double diff = getVideoClock() - audioFrame.Pts;
                if (diff > 0.2 && diff < 3 && syncMode == SyncMode.AUDIO_SYNCS_TO_VIDEO)
                {
                    //log.Warn("dropping audio buffer, lagging behind: " + (getVideoClock() - audioFrame.Pts).ToString() + " seconds");
                    goto restartaudio;
                }

                //adjustAudioSamplesPerSecond(audioFrame);
                adjustAudioLength(audioFrame);

                audioPlayer.play(audioFrame);

                int frameLength = audioFrame.Length;

                actualDelay = synchronizeAudio(frameLength);

                if (actualDelay < 0)
                {
                    // delay too small, play next frame as quickly as possible          
                    goto restartaudio;

                }

            }

            // start timer with delay for next frame
            audioRefreshTimer.Interval = (int)(actualDelay * 1000 + 0.5);
            audioRefreshTimer.start();
        }

        double synchronizeAudio(int frameLength)
        {

            // calculate delay to play next frame
            int bytesPerSecond = audioPlayer.SamplesPerSecond * videoDecoder.BytesPerSample * videoDecoder.NrChannels;

            double delay = frameLength / (double)bytesPerSecond;

            // adjust delay based on the actual current time
            audioFrameTimer += delay;
            double actualDelay = audioFrameTimer - HRTimer.getTimestamp();
      	
            return (actualDelay);
        }


        void adjustAudioSamplesPerSecond(VideoLib.AudioFrame frame)
        {

            //videoDebug.AudioFrameLengthAdjust = 0;

            if (syncMode == SyncMode.AUDIO_SYNCS_TO_VIDEO)
            {

                int n = videoDecoder.NrChannels * videoDecoder.BytesPerSample;

                double diff = audioPlayer.getAudioClock() - getVideoClock();

                if (Math.Abs(diff) < AV_NOSYNC_THRESHOLD)
                {
                    // accumulate the diffs
                    audioDiffCum = diff + audioDiffAvgCoef * audioDiffCum;

                    if (audioDiffAvgCount < AUDIO_DIFF_AVG_NB)
                    {
                        audioDiffAvgCount++;
                    }
                    else
                    {
                        double avgDiff = audioDiffCum * (1.0 - audioDiffAvgCoef);

                        // Shrinking/expanding buffer code....
                        if (Math.Abs(avgDiff) >= audioDiffThreshold)
                        {

                            int wantedSize = (int)(frame.Length + diff * videoDecoder.SamplesPerSecond * n);

                            // get a correction percent from 10 to 60 based on the avgDiff
                            // in order to converge a little faster
                            double correctionPercent = Utils.clamp(10 + (Math.Abs(avgDiff) - audioDiffThreshold) * 15, 10, 60);

                            //Util.DebugOut(correctionPercent);
                 
                            int minSize = (int)(frame.Length * ((100 - correctionPercent) / 100));
                            int maxSize = (int)(frame.Length * ((100 + correctionPercent) / 100));

                            if (wantedSize < minSize)
                            {
                                wantedSize = minSize;
                            }
                            else if (wantedSize > maxSize)
                            {
                                wantedSize = maxSize;
                            }

                            // adjust samples per second to speed up or slow down the audio
                            Int64 length = frame.Length;
                            Int64 sps = videoDecoder.SamplesPerSecond;
                            int samplesPerSecond = (int)((length * sps) / wantedSize);
                            //videoDebug.AudioFrameLengthAdjust = samplesPerSecond;
                            audioPlayer.SamplesPerSecond = samplesPerSecond;
                        }
                        else
                        {
                            audioPlayer.SamplesPerSecond = videoDecoder.SamplesPerSecond;
                        }

                    }

                }
                else
                {

                    // difference is TOO big; reset diff stuff 
                    audioDiffAvgCount = 0;
                    audioDiffCum = 0;
                }
            }

        }


        void adjustAudioLength(VideoLib.AudioFrame frame)
        {

            //videoDebug.AudioFrameLengthAdjust = 0;

            if (syncMode == SyncMode.AUDIO_SYNCS_TO_VIDEO)
            {

                int n = videoDecoder.NrChannels * videoDecoder.BytesPerSample;

                double diff = audioPlayer.getAudioClock() - getVideoClock();

                if (Math.Abs(diff) < AV_NOSYNC_THRESHOLD)
                {

                    // accumulate the diffs
                    audioDiffCum = diff + audioDiffAvgCoef * audioDiffCum;

                    if (audioDiffAvgCount < AUDIO_DIFF_AVG_NB)
                    {

                        audioDiffAvgCount++;

                    }
                    else
                    {

                        double avgDiff = audioDiffCum * (1.0 - audioDiffAvgCoef);

                        // Shrinking/expanding buffer code....
                        if (Math.Abs(avgDiff) >= audioDiffThreshold)
                        {

                            int wantedSize = (int)(frame.Length + diff * videoDecoder.SamplesPerSecond * n);

                            // get a correction percent from 10 to 60 based on the avgDiff
                            // in order to converge a little faster
                            double correctionPercent = Utils.clamp(10 + (Math.Abs(avgDiff) - audioDiffThreshold) * 15, 10, 60);

                            //Util.DebugOut(correctionPercent);

                            //AUDIO_SAMPLE_CORRECTION_PERCENT_MAX

                            int minSize = (int)(frame.Length * ((100 - correctionPercent)
                                / 100));

                            int maxSize = (int)(frame.Length * ((100 + correctionPercent)
                                / 100));

                            if (wantedSize < minSize)
                            {

                                wantedSize = minSize;

                            }
                            else if (wantedSize > maxSize)
                            {

                                wantedSize = maxSize;
                            }

                            // make sure the samples stay aligned after resizing the buffer
                            wantedSize = (wantedSize / n) * n;

                            if (wantedSize < frame.Length)
                            {

                                // remove samples 
                                //videoDebug.AudioFrameLengthAdjust = wantedSize - frame.Length;
                                frame.Length = wantedSize;                                

                            }
                            else if (wantedSize > frame.Length)
                            {

                                // add samples by copying final samples
                                int nrExtraSamples = wantedSize - frame.Length;
                                //videoDebug.AudioFrameLengthAdjust = nrExtraSamples;

                                byte[] lastSample = new byte[n];

                                for (int i = 0; i < n; i++)
                                {

                                    lastSample[i] = frame.Data[frame.Length - n + i];
                                }

                                frame.Stream.Position = frame.Length;

                                while (nrExtraSamples > 0)
                                {

                                    frame.Stream.Write(lastSample, 0, n);
                                    nrExtraSamples -= n;
                                }

                                frame.Stream.Position = 0;
                                frame.Length = wantedSize;
                            }

                        }

                    }

                }
                else
                {

                    // difference is TOO big; reset diff stuff 
                    audioDiffAvgCount = 0;
                    audioDiffCum = 0;
                }
            }

        }

        void videoDecoderLogCallback(int level, string message)
        {
            if (Log == null) return;

            if (level < 16)
            {
                Log.Fatal(message);
            }
            else if (level == 16)
            {
                Log.Error(message);
            }
            else if (level == 24)
            {
                Log.Warn(message);
            }
            else if (level == 48)
            {
                Log.Debug(message);
            }
            else
            {
                Log.Info(message);
            }
        }
                       
        double getVideoClock()
        {        
            if (videoState == VideoState.PAUSED || videoDecoder.FrameQueue.IsBuffering)
            {
                return (videoPts);
            }
            else
            {
                return (videoPtsDrift - HRTimer.getTimestamp());
            }
        }
        
        

        void startDemuxing()
        {        
            demuxPacketsCancellationTokenSource = new CancellationTokenSource();

            if (videoDecoder.HasSeperateAudioStream)
            {
                // video and audio is in seperate input streams
                demuxPacketsTask = new Task[2];

                demuxPacketsTask[0] = new Task(() => { demuxPackets(0, demuxPacketsCancellationTokenSource.Token); },
                    demuxPacketsCancellationTokenSource.Token, TaskCreationOptions.LongRunning);
                demuxPacketsTask[1] = new Task(() => { demuxPackets(1, demuxPacketsCancellationTokenSource.Token); },
                    demuxPacketsCancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            }
            else
            {
                demuxPacketsTask = new Task[1];

                // video and audio is in the same input stream
                demuxPacketsTask[0] = new Task(() => { demuxPackets(-1, demuxPacketsCancellationTokenSource.Token); },
                    demuxPacketsCancellationTokenSource.Token, TaskCreationOptions.LongRunning);
            }

            foreach (Task t in demuxPacketsTask)
            {
                t.Start();
            }

        }

        void demuxPackets(int i, CancellationToken token)
        {
            VideoLib.VideoPlayer.DemuxPacketsResult result = VideoLib.VideoPlayer.DemuxPacketsResult.SUCCESS;

            do
            {
                if (i == -1)
                {
                    result = videoDecoder.demuxPacketInterleaved();
                }
                else
                {
                    result = videoDecoder.demuxPacketFromStream(i);
                }

            } while (result != VideoLib.VideoPlayer.DemuxPacketsResult.STOPPED && !token.IsCancellationRequested);
        }

        public void seek(double positionSeconds)
        {
            if (VideoState == VideoPlayerControl.VideoState.CLOSED)
            {
                return;
            } 

            // wait for video and audio decoding to block
            // To make sure no packets are in limbo
            // before flushing any ffmpeg internal or external queues. 
            videoDecoder.FrameQueue.setState(FrameQueue.FrameQueueState.BLOCK,FrameQueue.FrameQueueState.BLOCK,
                FrameQueue.FrameQueueState.BLOCK);
                      
            if (videoDecoder.seek(positionSeconds) == true)
            {
                // flush the framequeue	and audioplayer buffer				
                videoDecoder.FrameQueue.flush();
                audioPlayer.flush();
            
                audioFrameTimer = videoFrameTimer = HRTimer.getTimestamp();
            }
           
            // buffer and allow video and audio decoding to continue   
            //await videoDecoder.FrameQueue.startPacketQueueBuffering();
            if (VideoState == VideoPlayerControl.VideoState.PLAYING)
            {
                videoDecoder.FrameQueue.setState(FrameQueue.FrameQueueState.PLAY,
                    FrameQueue.FrameQueueState.PLAY, FrameQueue.FrameQueueState.PLAY);
            }
            else if (VideoState == VideoPlayerControl.VideoState.PAUSED)
            {
                // display the first new frame in paused mode
                videoDecoder.FrameQueue.startSingleFrame();
            }
        }

        public void displayNextFrame()
        {         
            if (VideoState == VideoPlayerControl.VideoState.PLAYING) 
            {            
                pause();
            }
            else if(VideoState == VideoPlayerControl.VideoState.PAUSED)
            {                                             
                videoDecoder.FrameQueue.startSingleFrame();                              
            }
        }

        public void pause()
        {

            if (VideoState == VideoState.PAUSED ||
                VideoState == VideoState.CLOSED)
            {
                return;
            }

            VideoState = VideoState.PAUSED;

            videoDecoder.FrameQueue.setState(FrameQueue.FrameQueueState.PAUSE, FrameQueue.FrameQueueState.PAUSE,
                FrameQueue.FrameQueueState.PLAY);
        }

        public void play()
        {
            if (VideoState == VideoState.PLAYING ||
                VideoState == VideoState.CLOSED)
            {
                return;
            }

            audioFrameTimer = videoFrameTimer = HRTimer.getTimestamp();

            videoDecoder.FrameQueue.setState(FrameQueue.FrameQueueState.PLAY, FrameQueue.FrameQueueState.PLAY,
                FrameQueue.FrameQueueState.PLAY);

            if (VideoState == VideoState.OPEN)
            {
                startDemuxing();

                if(videoDecoder.HasVideo) videoRefreshTimer.start();
                if(videoDecoder.HasAudio) audioRefreshTimer.start();
            }

            VideoState = VideoState.PLAYING;

        }
             
        public async Task openAndPlay(string location,        
            string inputFormatName = null, string audioLocation = null,
            string audioFormatName = null)
        {
            try
            {
                // cancel any previously running open operation
                CancellationTokenSource prevCancelTokenSource = CancelTokenSource;
                Task prevOpenTask = OpenTask;

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();                
                CancelTokenSource = newCancelTokenSource;

                await close(prevCancelTokenSource, prevOpenTask);

                // open video
                newCancelTokenSource.Token.ThrowIfCancellationRequested();

                VideoLocation = location;

                OpenTask = Task.Factory.StartNew(() => videoDecoder.open(location, DecodedVideoFormat, inputFormatName, audioLocation, audioFormatName, newCancelTokenSource.Token), newCancelTokenSource.Token);
                await OpenTask;

                if (videoDecoder.HasVideo)
                {
                    videoRender.initialize(videoDecoder.Width, videoDecoder.Height);
                }

                DurationSeconds = videoDecoder.DurationSeconds;
                HasAudio = videoDecoder.HasAudio;

                previousVideoPts = 0;
                previousVideoDelay = 0.04;
                audioDiffAvgCount = 0;

                if (videoDecoder.HasAudio)
                {
                    audioPlayer.initialize(videoDecoder.SamplesPerSecond, videoDecoder.BytesPerSample,
                        Math.Min(videoDecoder.NrChannels, 2), videoDecoder.MaxAudioFrameSize * 2);

                    audioDiffThreshold = 2.0 * 1024 / videoDecoder.SamplesPerSecond;
                }

                VideoState = VideoState.OPEN;

                play();

            }
            catch (VideoLib.VideoLibException)
            {

                VideoState = VideoState.CLOSED;

                //log.Error("Cannot open: " + location, e);

                throw;

            }
                       
        }

        async Task close(CancellationTokenSource interruptIOTokenSource, Task openTask)
        {          
            // cancel any running open operation
            if (interruptIOTokenSource != null)
            {
                interruptIOTokenSource.Cancel();
                try
                {
                    if (!openTask.IsCompleted)
                    {
                        await openTask;
                    }
                }
                catch (OperationCanceledException)
                {

                }
            }
           
            if (VideoState == VideoState.CLOSED)
            {
                return;
            }

            VideoState = VideoState.CLOSED;

            videoDecoder.FrameQueue.setState(FrameQueue.FrameQueueState.CLOSE,
                FrameQueue.FrameQueueState.CLOSE, FrameQueue.FrameQueueState.CLOSE);

            demuxPacketsCancellationTokenSource.Cancel();

            await Task.WhenAll(demuxPacketsTask);

            videoDecoder.close();

            
            
            videoPts = 0;
            videoPtsDrift = 0;

            DurationSeconds = 0;
            PositionSeconds = 0;

            NrFramesDropped = 0;
            NrFramesRendered = 0;

            VideoLocation = "";
        }

        public async Task close()
        {
            await close(CancelTokenSource, OpenTask);

        }

        public void resize()
        {
            videoRender.resize();           
        }

        public void toggleFullScreen()
        {
            if (videoRender.Windowed == true)
            {
                videoRender.setFullScreen();
            }
            else
            {
                videoRender.setWindowed();
            }
        }
    }

}
