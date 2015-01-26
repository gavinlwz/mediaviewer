﻿using MediaViewer.Infrastructure.Logging;
using MediaViewer.Logging;
using MediaViewer.MediaDatabase;
using MediaViewer.MediaDatabase.DbCommands;
using MediaViewer.MetaData;
using MediaViewer.Model.Media.Metadata;
using MediaViewer.Model.Media.State;
using MediaViewer.Model.Utils;
using MediaViewer.Progress;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaViewer.Model.Media.File
{
    public class MediaFactory
    {
        static int maxConcurrentReads = 10;
        static SemaphoreSlim limitConcurrentReadsSemaphore = new SemaphoreSlim(maxConcurrentReads, maxConcurrentReads);
       

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
        // 5 seconds
        const int FILE_OPEN_ASYNC_TIMEOUT_MS = 5 * 1000;
        // 5 seconds
        const int FILE_OPEN_SYNC_TIMEOUT_MS = 5 * 1000;
        
        static BaseMedia readMediaFromWeb(string location, ReadOptions options, CancellationToken token, int timeoutSeconds)
        {

            HttpWebResponse response = null;
            Stream responseStream = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(location);
                request.Method = "GET";
                request.Timeout = timeoutSeconds * 1000;

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

                BaseMedia media = createMediaFromMimeType(location, options, response.ContentType, data, token, timeoutSeconds);

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

        static BaseMedia readMediaFromFile(String location, ReadOptions options, CancellationToken token, int timeoutSeconds)
        {

            Stream data = FileUtils.waitForFileAccess(location, FileAccess.Read,
                FILE_OPEN_ASYNC_TIMEOUT_MS, token);

            string mimeType = MediaFormatConvert.fileNameToMimeType(location);

            BaseMedia media = createMediaFromMimeType(location, options, mimeType, data, token, timeoutSeconds);

            return (media);
        }

        static BaseMedia createMediaFromMimeType(String location, ReadOptions options,
            string mimeType, Stream data, CancellationToken token, int timeoutSeconds)
        {

            BaseMedia media = null;

            if (mimeType.ToLower().StartsWith("image"))
            {
                media = new ImageMedia(location, data);
                ImageMetadataReader imageMetaData = new ImageMetadataReader();
                imageMetaData.readMetadata(data, options, media, token, timeoutSeconds);

            }
            else if (mimeType.ToLower().StartsWith("video"))
            {
                media = new VideoMedia(location, data);
                VideoMetadataReader videoMetaData = new VideoMetadataReader();
                videoMetaData.readMetadata(data, options, media, token, timeoutSeconds);

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

        private static BaseMedia readMediaFromDatabase(string location, ReadOptions options, CancellationToken token, int timeoutSeconds)
        {
            BaseMedia media = null;

            using (MediaDbCommands mediaCommands = new MediaDbCommands())
            {
                media = mediaCommands.findMediaByLocation(location);

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

                    // check if media stored in the database is outdated                
                    FileInfo info = new FileInfo(media.Location);
                    info.Refresh();

                    if (info.Exists == false)
                    {
                        media.MetadataReadError = new FileNotFoundException("File not found", media.Location);
                        return (media);
                    }

                    if ((info.LastWriteTime - media.LastModifiedDate) > TimeSpan.FromSeconds(10))
                    {
                        // media is outdated so update in database
                        Logger.Log.Info("Updated: " + media.Location + " - Database timestamp: " + media.LastModifiedDate.ToString() + " Disk timestamp: " + info.LastWriteTime.ToString());
                        int id = media.Id;
                        media = readMediaFromFile(media.Location, options, token, timeoutSeconds);

                        if (media != null)
                        {
                            media.IsImported = true;
                            media.Id = id;
                            write(media, WriteOptions.WRITE_TO_DATABASE, null);
                        }

                    }

                }
            }

            return (media);
        }

        public static void write(BaseMedia media, WriteOptions options, ICancellableOperationProgress progress)
        {
           
            if (options.HasFlag(WriteOptions.AUTO) || options.HasFlag(WriteOptions.WRITE_TO_DISK))
            {

                if (media.MimeType.ToLower().StartsWith("image"))
                {
                    ImageMetadataWriter imageMetadataWriter = new ImageMetadataWriter();
                    imageMetadataWriter.writeMetadata(media, progress);

                }
                else if (media.MimeType.ToLower().StartsWith("video"))
                {
                    VideoMetadataWriter videoMetadataWriter = new VideoMetadataWriter();
                    videoMetadataWriter.writeMetadata(media, progress);

                }
            }

            if (media.IsImported && (options.HasFlag(WriteOptions.AUTO) || options.HasFlag(WriteOptions.WRITE_TO_DATABASE)))
            {
                using (MediaDbCommands mediaCommands = new MediaDbCommands())
                {
                    media = mediaCommands.update(media);
                }
            }

            media.IsModified = false;
            
        }
       

        /// <summary>
        /// Returns null when media does not exist
        /// UnknownMedia when media is not recognized
        /// </summary>
        /// <param name="location"></param>
        /// <param name="options"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static BaseMedia read(string location, ReadOptions options, CancellationToken token, int timeoutSeconds = 60)
        {

            limitConcurrentReadsSemaphore.Wait(token);
            try
            {
                // initialize media with a dummy in case of exceptions
                BaseMedia media = null;

                if (string.IsNullOrEmpty(location) || token.IsCancellationRequested == true)
                {
                    return (media);
                }
                else if (FileUtils.isUrl(location))
                {
                    media = readMediaFromWeb(location, options, token, timeoutSeconds);
                }
                else
                {
                    if (options.HasFlag(ReadOptions.AUTO) || options.HasFlag(ReadOptions.READ_FROM_DATABASE))
                    {

                        media = readMediaFromDatabase(location, options, token, timeoutSeconds);
                    }

                    if ((media == null && options.HasFlag(ReadOptions.AUTO)) ||
                        options.HasFlag(ReadOptions.READ_FROM_DISK))
                    {
                        media = readMediaFromFile(location, options, token, timeoutSeconds);
                    }
                }

                media.IsModified = false;
                return (media);

            }
            catch (OperationCanceledException)
            {
                // convert operationcanceled to taskcanceled when the token aborts waiting for the semaphore
                throw new TaskCanceledException();
            }
            finally
            {
                limitConcurrentReadsSemaphore.Release();
            }
        }



    }
}
