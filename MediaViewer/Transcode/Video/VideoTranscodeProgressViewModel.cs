﻿using MediaViewer.Model.Concurrency;
using MediaViewer.Model.Media.File;
using MediaViewer.Model.Mvvm;
using MediaViewer.Model.Utils;
using MediaViewer.Progress;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;


namespace MediaViewer.Transcode.Video
{
    class VideoTranscodeProgressViewModel : CancellableOperationProgressBase
    {
        VideoTranscodeViewModel AsyncState { get; set; }
        ICollection<MediaFileItem> Items { get; set; }

        public VideoTranscodeProgressViewModel(VideoTranscodeViewModel vm)
        {
            WindowIcon = "pack://application:,,,/Resources/Icons/videotranscode.ico";
            WindowTitle = "Video Transcoding Progress";

            AsyncState = vm;
            Items = vm.Items;

            CancelCommand.IsExecutable = true;
            OkCommand.IsExecutable = false;
                    
        }

        public async Task startTranscodeAsync()
        {
            await Task.Factory.StartNew(() =>
            {
                startTranscode();

            },CancellationToken, TaskCreationOptions.None, PriorityScheduler.BelowNormal);

            OkCommand.IsExecutable = true;
            CancelCommand.IsExecutable = false;

        }

        Dictionary<String, Object> getOptions()
        {
            Dictionary<String, Object> options = new Dictionary<string, object>();
            options.Add("videoStreamMode", AsyncState.VideoStreamMode);
            options.Add("audioStreamMode", AsyncState.AudioStreamMode);
            options.Add("videoEncoder", AsyncState.VideoEncoder);
            options.Add("audioEncoder", AsyncState.AudioEncoder);
            options.Add("videoEncoderPreset", AsyncState.VideoEncoderPreset);

            if (AsyncState.Width.HasValue)
            {
                options.Add("width", AsyncState.Width.Value);
            }

            if (AsyncState.Height.HasValue)
            {
                options.Add("height", AsyncState.Height.Value);
            }

            if (AsyncState.SampleRate.HasValue)
            {
                options.Add("sampleRate", AsyncState.SampleRate.Value);
            }

            if (AsyncState.NrChannels.HasValue)
            {
                options.Add("nrChannels", AsyncState.NrChannels.Value);
            }

            if (AsyncState.IsTimeRangeEnabled)
            {
                options.Add("startTimeRange", AsyncState.StartTimeRange);
                options.Add("endTimeRange", AsyncState.EndTimeRange);
            }

            return (options);
        }

        void logCallback(int logLevel, String message)
        {
            InfoMessages.Add(message);
        }

        void startTranscode()
        {
            
            VideoLib.VideoTranscoder videoTranscoder = new VideoLib.VideoTranscoder();
            videoTranscoder.setLogCallback(logCallback, true, VideoLib.VideoTranscoder.LogLevel.LOG_LEVEL_INFO);
            TotalProgressMax = Items.Count;
            TotalProgress = 0;
            ItemProgressMax = 100;
           
            Dictionary<String, Object> options = getOptions();
                
            foreach (MediaFileItem input in Items)
            {
                ItemProgress = 0;
                ItemInfo = "Transcoding: " + input.Location;

                if (CancellationToken.IsCancellationRequested) break;
                if (!MediaFormatConvert.isVideoFile(input.Location))
                {
                    InfoMessages.Add("Skipping: " + input.Location + " is not a video file.");
                    TotalProgress++;
                    continue;
                }

                String outLocation = AsyncState.OutputPath + "\\" + Path.GetFileNameWithoutExtension(input.Location);

                outLocation += "." + AsyncState.ContainerFormat.ToString().ToLower();
                                       
                outLocation = FileUtils.getUniqueFileName(outLocation);

                try
                {
                    videoTranscoder.transcode(input.Location, outLocation, CancellationToken, options,
                        transcodeProgressCallback);
                }
                catch (Exception e)
                {
                    InfoMessages.Add("Error transcoding: " + e.Message);

                    try
                    {
                        File.Delete(outLocation);
                    }
                    catch (Exception ex)
                    {
                        InfoMessages.Add("Error deleting: " + outLocation + " " + ex.Message);
                    }
                    
                    return;
                }
                    
                ItemProgress = 100;
                TotalProgress++;

                InfoMessages.Add("Finished Transcoding: " + input.Location + " -> " + outLocation);
            }

        }

        void transcodeProgressCallback(double progress)
        {
            ItemProgress = (int)(progress * 100);
        }
       
    }
}
