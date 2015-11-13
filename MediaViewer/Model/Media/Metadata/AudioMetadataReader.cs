﻿using MediaViewer.Infrastructure;
using MediaViewer.Infrastructure.Logging;
using MediaViewer.MediaDatabase;
using MediaViewer.Model.metadata.Metadata;
using MediaViewer.Model.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VideoLib;

namespace MediaViewer.Model.Media.Metadata
{
    class AudioMetadataReader : MetadataReader
    {
        public override void readMetadata(Stream data, MetadataFactory.ReadOptions options, BaseMetadata media,
            CancellationToken token, int timeoutSeconds)
        {
            AudioMetadata audio = media as AudioMetadata;

            VideoPreview videoPreview = null;
            List<String> fsMetaData = null;

            try
            {
                try
                {
                    videoPreview = new VideoPreview();
                    videoPreview.open(media.Location, token);

                    audio.DurationSeconds = videoPreview.DurationSeconds;
                    audio.SizeBytes = videoPreview.SizeBytes;
                                              
                    if (!String.IsNullOrEmpty(videoPreview.AudioCodecName))
                    {
                        audio.AudioCodec = videoPreview.AudioCodecName;
                        audio.SamplesPerSecond = videoPreview.SamplesPerSecond;
                        audio.BitsPerSample = (short)(videoPreview.BytesPerSample * 8);
                        audio.NrChannels = (short)videoPreview.NrChannels;
                    }
                    

                    fsMetaData = videoPreview.MetaData;

                }
                catch (Exception e)
                {
                    Logger.Log.Error("FFMPG cannot read audio file: " + audio.Location, e);
                    media.MetadataReadError = e;
                }

                try
                {
                    if (videoPreview != null)
                    {
                        if (options.HasFlag(MetadataFactory.ReadOptions.GENERATE_THUMBNAIL) ||
                            options.HasFlag(MetadataFactory.ReadOptions.GENERATE_MULTIPLE_THUMBNAILS))
                        {
                            generateThumbnail(videoPreview, audio, token, timeoutSeconds, 1);
                        }                        
                    }
                }
                catch (Exception e)
                {
                    Logger.Log.Error("Cannot read audio thumbnail: " + audio.Location, e);
                    media.MetadataReadError = e;
                }

                if(audio.AudioCodec.ToLower().Equals("mp3") || audio.AudioCodec.ToLower().StartsWith("pcm"))
                {
                    audio.SupportsXMPMetadata = true;
                    base.readMetadata(data, options, media, token, timeoutSeconds);
                }
                else
                {
                    audio.SupportsXMPMetadata = false;
                }
                               
                parseFFMpegMetaData(fsMetaData, audio);

            }
            finally
            {
                if (videoPreview != null)
                {
                    videoPreview.close();
                    videoPreview.Dispose();
                }
            }
           
        }

        public void generateThumbnail(VideoPreview videoPreview, AudioMetadata audio,
            CancellationToken token, int timeoutSeconds, int nrThumbnails)
        {
          
            // possibly could not seek in video, try to get the first frame in the video
            List<VideoThumb> thumbBitmaps = videoPreview.grabThumbnails(Constants.MAX_THUMBNAIL_WIDTH,
                Constants.MAX_THUMBNAIL_HEIGHT, -1, 1, 0, token, timeoutSeconds);
           
            audio.Thumbnails.Clear();

            foreach (VideoThumb videoThumb in thumbBitmaps)
            {
                audio.Thumbnails.Add(new Thumbnail(videoThumb.Thumb, videoThumb.PositionSeconds));
            }

        }

        static List<String> encoderMatch = new List<String>() { "encoder", "encoded_with", "encoded_by"};
        static List<String> descriptionMatch = new List<string>() { "description", "comment" };
        static List<String> authorMatch = new List<string>() { "artist", "composer"};

        void parseFFMpegMetaData(List<string> fsMetaData, AudioMetadata audio)
        {
            if (fsMetaData == null) return;

            foreach (String info in fsMetaData)
            {
                string[] temp = info.Split(new char[] { ':' }, 2);

                if (temp != null)
                {
                    String param = temp[0].ToLower();
                    String value = temp[1].Trim();

                    if (String.IsNullOrEmpty(value) || String.IsNullOrWhiteSpace(value)) continue;

                    // Note that when setting the title like this, if the user clears the (XMP) title it will 
                    // revert to the title stored in the ffmpeg metadata. This will be confusing for the user
                    // and should probably be fixed.
                    if (audio.Title == null && param.Equals("title"))
                    {
                        audio.Title = value;
                    }
                    else if (audio.Description == null && descriptionMatch.Any(s => s.Equals(param)))
                    {
                        audio.Description = value;
                    }
                    else if (audio.Author == null && authorMatch.Any(s => s.Equals(param)))
                    {
                        audio.Author = value;
                    }
                    else if (audio.Copyright == null && param.Equals("copyright"))
                    {
                        audio.Copyright = value;
                    }
                    else if (audio.Software == null && encoderMatch.Any(s => s.Equals(param)))
                    {
                        audio.Software = value;
                    }
                    else if (audio.Genre == null && param.Equals("genre"))
                    {
                        audio.Genre = value;
                    }
                    else if (audio.Album == null && param.Equals("album"))
                    {
                        audio.Album = value;
                    }
                    else if (audio.TrackNr == null && param.Equals("track"))
                    {
                        int seperator = value.IndexOf('/');

                        int trackNr, totalTracks;
                        bool success;

                        if (seperator != -1)
                        {
                            string[] trackInfo = value.Split(new char[] { '/' }, 2);
                            value = trackInfo[0].Trim();

                            success = Int32.TryParse(trackInfo[1].Trim(), out totalTracks);
                            if (success)
                            {
                                audio.TotalTracks = totalTracks;
                            }                            
                        }
                        
                        success = Int32.TryParse(value, out trackNr);
                        if (success)
                        {
                            audio.TrackNr = trackNr;
                        }
                    }
                    else if (audio.TotalTracks == null && param.Equals("tracktotal"))
                    {
                        int totalTracks;
                        bool success = Int32.TryParse(value, out totalTracks);
                        if (success)
                        {
                            audio.TotalTracks = totalTracks;
                        }
                    }
                    else if (audio.DiscNr == null && param.Equals("disc"))
                    {
                        int seperator = value.IndexOf('/');

                        int discNr, totalDiscs;
                        bool success;

                        if (seperator != -1)
                        {
                            string[] discInfo = value.Split(new char[] { '/' }, 2);
                            value = discInfo[0].Trim();

                            success = Int32.TryParse(discInfo[1].Trim(), out totalDiscs);
                            if (success)
                            {
                                audio.TotalDiscs = totalDiscs;
                            }
                        }

                        success = Int32.TryParse(value, out discNr);
                        if (success)
                        {
                            audio.DiscNr = discNr;
                        }
                    }
                    else if (audio.TotalTracks == null && param.Equals("disctotal"))
                    {
                        int totalDiscs;
                        bool success = Int32.TryParse(value, out totalDiscs);
                        if (success)
                        {
                            audio.TotalDiscs = totalDiscs;
                        }
                    }
                                                      
                }
            }
        }
    }
}
