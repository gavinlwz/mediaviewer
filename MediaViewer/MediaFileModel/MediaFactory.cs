﻿using MediaViewer.MediaDatabase;
using MediaViewer.MediaDatabase.DbCommands;
using MediaViewer.MetaData;
using MediaViewer.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaViewer.MediaFileModel
{
    public class MediaFactory
    {
        public enum ReadOptions
        {
            AUTO = 0x1,
            READ_FROM_DISK = 0x2,
            READ_FROM_DATABASE = 0x4,      
            GENERATE_THUMBNAIL = 0x8,
            LEAVE_STREAM_OPENED_AFTER_READ = 0x10
        }

        public enum WriteOptions
        {
            AUTO = 0x1,
            WRITE_TO_DISK = 0x2,
            WRITE_TO_DATABASE = 0x4
        }

        // 60 seconds
        const int HTTP_TIMEOUT_MS = 60 * 1000;
        const int HTTP_READ_BUFFER_SIZE_BYTES = 8096;
        // 1 hour
        const int FILE_OPEN_ASYNC_TIMEOUT_MS = 60 * 60 * 1000;
        // 5 seconds
        const int FILE_OPEN_SYNC_TIMEOUT_MS = 5 * 1000;

        static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static Media readMediaFromWeb(string location, ReadOptions options, CancellationToken token)
        {

            HttpWebResponse response = null;
            Stream responseStream = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(location);
                request.Method = "GET";
                request.Timeout = HTTP_TIMEOUT_MS;

                IAsyncResult requestResult = request.BeginGetResponse(null, null);

                while (!requestResult.IsCompleted)
                {

                    if (token.IsCancellationRequested)
                    {

                        request.Abort();
                        throw new MediaStateException("Aborting opening image");
                    }

                    Thread.Sleep(100);
                }

                response = (HttpWebResponse)request.EndGetResponse(requestResult);

                responseStream = response.GetResponseStream();
                responseStream.ReadTimeout = HTTP_TIMEOUT_MS;

                Stream data = new MemoryStream();

                int bufSize = HTTP_READ_BUFFER_SIZE_BYTES;
                int count = 0;

                byte[] buffer = new byte[bufSize];

                while ((count = responseStream.Read(buffer, 0, bufSize)) > 0)
                {

                    if (token.IsCancellationRequested)
                    {

                        throw new MediaStateException("Aborting reading image");
                    }

                    data.Write(buffer, 0, count);
                }

                data.Seek(0, System.IO.SeekOrigin.Begin);

                Media media = createMediaFromMimeType(location, options, response.ContentType, data);

                return (media);

            }
            finally
            {
                if (responseStream != null)
                {
                    responseStream.Close();
                }

                if (response != null)
                {

                    response.Close();
                }
            }
        }

        static Media readMediaFromFile(String location, ReadOptions options, CancellationToken token)
        {

            Stream data = FileUtils.waitForFileAccess(location, FileAccess.Read,
                FILE_OPEN_ASYNC_TIMEOUT_MS, token);

            string mimeType = MediaFormatConvert.fileNameToMimeType(location);

            Media media = createMediaFromMimeType(location, options, mimeType, data);

            return (media);
        }

        static Media createMediaFromMimeType(String location, ReadOptions options,
            string mimeType, Stream data)
        {

            Media media = null;

            if (mimeType.ToLower().StartsWith("image"))
            {
                media = new ImageMedia(location, data);
                ImageMetadataReader imageMetaData = new ImageMetadataReader();
                imageMetaData.readMetadata(data, options, media);

            }
            else if (mimeType.ToLower().StartsWith("video"))
            {
                media = new VideoMedia(location, data);
                VideoMetadataReader videoMetaData = new VideoMetadataReader();
                videoMetaData.readMetadata(data, options, media);

            }
            else
            {
                media = new UnknownMedia(location);
            }

            if(!options.HasFlag(ReadOptions.LEAVE_STREAM_OPENED_AFTER_READ)) {

                media.close();
            }

            return (media);
        }

        private static Media readMediaFromDatabase(string location, ReadOptions options, CancellationToken token)
        {
            MediaDbCommands mediaCommands = new MediaDbCommands();
          
            Media media = mediaCommands.findMediaByLocation(location);

            if (media != null)
            {
                media.IsImported = true;

                if (media.Thumbnail != null)
                {
                    media.Thumbnail.decodeImage();
                }

                if (options.HasFlag(ReadOptions.LEAVE_STREAM_OPENED_AFTER_READ))
                {
                    Stream data = FileUtils.waitForFileAccess(location, FileAccess.Read,
                        FILE_OPEN_ASYNC_TIMEOUT_MS, token);

                    media.Data = data;
                }
            }

            return (media);
        }

        public static void write(Media media, WriteOptions options)
        {
           
            if (options.HasFlag(WriteOptions.AUTO) || options.HasFlag(WriteOptions.WRITE_TO_DISK))
            {

                if (media.MimeType.ToLower().StartsWith("image"))
                {
                    ImageMetadataWriter imageMetadataWriter = new ImageMetadataWriter();
                    imageMetadataWriter.writeMetadata(media);

                }
                else if (media.MimeType.ToLower().StartsWith("video"))
                {
                    VideoMetadataWriter videoMetadataWriter = new VideoMetadataWriter();
                    videoMetadataWriter.writeMetadata(media);

                }
            }

            if (media.IsImported && (options.HasFlag(WriteOptions.AUTO) || options.HasFlag(WriteOptions.WRITE_TO_DATABASE)))
            {
                MediaDbCommands mediaCommands = new MediaDbCommands();
                media = mediaCommands.updateMedia(media);
            }
            
        }

        /// <summary>
        /// Returns null when media does not exist
        /// UnknownMedia when media is not recognized
        /// </summary>
        /// <param name="location"></param>
        /// <param name="options"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static Media read(string location, ReadOptions options, CancellationToken token)
        {
            // initialize media with a dummy in case of exceptions
            Media media = null;
           
            try
            {

                if (string.IsNullOrEmpty(location) || token.IsCancellationRequested == true)
                {
                    return(media);
                }
                else if (FileUtils.isUrl(location))
                {
                    media = readMediaFromWeb(location, options, token);
                }
                else
                {
                    if(options.HasFlag(ReadOptions.AUTO) || options.HasFlag(ReadOptions.READ_FROM_DATABASE)) {

                        media = readMediaFromDatabase(location, options, token);                        
                    }                

                    if ((media == null && options.HasFlag(ReadOptions.AUTO)) || 
                        options.HasFlag(ReadOptions.READ_FROM_DISK))
                    {
                        media = readMediaFromFile(location, options, token);
                      
                    }
                                                          
                }

            }
            catch (Exception e)
            {
                log.Warn("Error reading media: " + location, e);
                                     
            }
            
            return (media);
        }

     
        public static async Task<Media> readAsync(string location, ReadOptions options, CancellationToken token)
        {

            return await Task<Media>.Run(() => read(location, options, token), token).ConfigureAwait(false);
           

        }

    }
}
