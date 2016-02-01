﻿using MediaViewer.Infrastructure;
using MediaViewer.Infrastructure.Logging;
using MediaViewer.MediaDatabase;
using MediaViewer.Model.Media.Base;
using MediaViewer.Model.Media.Base.Metadata;
using MediaViewer.Model.Media.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using VideoLib;

namespace MediaViewer.Model.Media.File.Metadata
{
    class VideoFileMetadataReader : MetadataFileReader
    {
             
        public override void readMetadata(VideoPreview videoPreview, Stream data, MetadataFactory.ReadOptions options, BaseMetadata media, 
            CancellationToken token, int timeoutSeconds)
        {
            VideoMetadata video = media as VideoMetadata;
                                                           
            video.DurationSeconds = videoPreview.DurationSeconds;
            video.SizeBytes = videoPreview.SizeBytes;

            video.Width = videoPreview.Width;
            video.Height = videoPreview.Height;

            video.VideoContainer = videoPreview.Container;
            video.VideoCodec = videoPreview.VideoCodecName;

            video.PixelFormat = videoPreview.PixelFormat;
            video.BitsPerPixel = (short)videoPreview.BitsPerPixel;

            video.FramesPerSecond = videoPreview.FrameRate;

            if (!String.IsNullOrEmpty(videoPreview.AudioCodecName))
            {
                video.AudioCodec = videoPreview.AudioCodecName;
                video.SamplesPerSecond = videoPreview.SamplesPerSecond;
                video.BitsPerSample = (short)(videoPreview.BytesPerSample * 8);
                video.NrChannels = (short)videoPreview.NrChannels;
            }
            else
            {
                video.AudioCodec = null;
                video.SamplesPerSecond = null;
                video.BitsPerSample = null;
                video.NrChannels = null;
            }

            List<String> fsMetaData = videoPreview.MetaData;                   
               
            try
            {               
                if (options.HasFlag(MetadataFactory.ReadOptions.GENERATE_THUMBNAIL))
                {
                    generateThumbnail(videoPreview, video, token, timeoutSeconds, 1);
                }
                else if(options.HasFlag(MetadataFactory.ReadOptions.GENERATE_MULTIPLE_THUMBNAILS))
                {
                    generateThumbnail(videoPreview, video, token, timeoutSeconds, 16);
                }                
            }
            catch (Exception e)
            {
                Logger.Log.Error("Cannot create video thumbnail: " + video.Location, e);
                media.MetadataReadError = e;
            }
                
            if (fsMetaData != null)
            {
                video.SupportsXMPMetadata = supportsXMPMetadata(video, fsMetaData);
            }
            else
            {
                video.SupportsXMPMetadata = false;
            }

            base.readMetadata(videoPreview, data, options, media, token, timeoutSeconds);

            parseFFMpegMetaData(fsMetaData, video);
            
           
        }

        public void generateThumbnail(VideoPreview videoPreview, VideoMetadata video, 
            CancellationToken token, int timeoutSeconds, int nrThumbnails)
         {

             List<VideoThumb> thumbBitmaps = videoPreview.grabThumbnails(Constants.MAX_THUMBNAIL_WIDTH,
                  Constants.MAX_THUMBNAIL_HEIGHT, -1, nrThumbnails, 0.025, token, timeoutSeconds);
            
             if (thumbBitmaps.Count == 0)
             {

                 // possibly could not seek in video, try to get the first frame in the video
                 thumbBitmaps = videoPreview.grabThumbnails(Constants.MAX_THUMBNAIL_WIDTH,
                     Constants.MAX_THUMBNAIL_HEIGHT, -1, 1, 0, token, timeoutSeconds);
             }

             video.Thumbnails.Clear();

             foreach (VideoThumb videoThumb in thumbBitmaps)
             {
                 video.Thumbnails.Add(new Thumbnail(videoThumb.Thumb, videoThumb.PositionSeconds));
             }
                        
         }

         static List<String> encoderMatch = new List<String>() { "encoder", "wm/toolname", "encoded_with", "encoded_by"};
         static List<String> descriptionMatch = new List<string>() {"description","comment"};
         static List<String> authorMatch = new List<string>() {"artist","author"};

         void parseFFMpegMetaData(List<string> fsMetaData, VideoMetadata video)
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
                     if (video.Title == null && param.Equals("title"))
                     {
                         video.Title = value;
                     }
                     else if (video.Description == null && descriptionMatch.Any(s => s.Equals(param)))
                     {
                         video.Description = value;
                     }
                     else if (video.Author == null && authorMatch.Any(s => s.Equals(param)))
                     {
                         video.Author = value;
                     }
                     else if (video.Copyright == null && param.Equals("copyright"))
                     {
                         video.Copyright = value;
                     }
                     else if (video.Software == null && encoderMatch.Any(s => s.Equals(param)))
                     {
                         video.Software = value;
                     }
                     else if (param.Equals("major_brand"))
                     {
                         video.MajorBrand = value;
                     }
                     else if (param.Equals("minor_version"))
                     {
                         int minorVersion;
                         if(int.TryParse(value, out minorVersion)) {

                            video.MinorVersion = minorVersion;
                         }
                     }
                     else if (param.Equals("wmfsdkversion"))
                     {
                         video.WMFSDKVersion = value;
                     }
                     else if (param.Equals("isvbr"))
                     {
                         bool isVBR;
                         if (bool.TryParse(value, out isVBR))
                         {
                             video.IsVariableBitRate = isVBR;
                         }
                     }
                  
                 }
             }
         }

         private bool supportsXMPMetadata(VideoMetadata video, List<string> fsMetaData)                  
         {
            
             // XMP Metadata does not support matroska
             if (video.MimeType.Equals("video/x-matroska") || video.MimeType.Equals("video/webm"))
             {
                 return (false);                
             }
             else if (video.MimeType.Equals("video/mp4"))
             {
                 // mp4 versions incompatible with XMP metadata

                 if (fsMetaData.Contains("major_brand: isom") &&
                     fsMetaData.Contains("minor_version: 1"))
                 {
                     return (false);
                 }

                 if (fsMetaData.Contains("major_brand: mp42") &&
                     fsMetaData.Contains("minor_version: 0"))
                 {

                     if (fsMetaData.Contains("compatible_brands: isom"))
                     {
                         return (false);
                     }

                     if (fsMetaData.Contains("compatible_brands: 000000964375"))
                     {
                         return (false);
                     }
                 }

             }
             else if (video.MimeType.Equals("video/avi"))
             {

                 if (video.VideoCodec.Equals("mpeg2video"))
                 {

                     return (false);
                 }
             }

             return (true);
         }
    }
}
