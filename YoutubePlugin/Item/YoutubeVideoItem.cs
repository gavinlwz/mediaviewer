﻿using Google.Apis.Requests;
using Google.Apis.YouTube.v3.Data;
using MediaViewer.MediaDatabase;
using MediaViewer.Model.Media.Base;
using MediaViewer.Model.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media.Imaging;

namespace YoutubePlugin.Item
{
    class YoutubeVideoItem : YoutubeItem
    {
        public bool IsEmbeddedOnly { get; set; }

        public List<YoutubeVideoStreamedItem> StreamedItem { get; set; }
                                
        public YoutubeVideoItem(IDirectResponseSchema result, int relevance) :
            base(result, relevance)
        {
            IsEmbeddedOnly = false;

            Info = result;

            StreamedItem = new List<YoutubeVideoStreamedItem>();                
        }

        public void getBestQualityStreams(out YoutubeVideoStreamedItem video, out YoutubeVideoStreamedItem audio) {

            video = null;
            audio = null;

            int bestHeight = 0;
            int bestSamplesPerSecond = 0;

            RWLock.EnterReadLock();

            try
            {
                // get best video stream
                foreach (YoutubeVideoStreamedItem item in StreamedItem)
                {
                    VideoMetadata metaData = item.Metadata as VideoMetadata;

                    if (metaData.Height >= bestHeight)
                    {                        
                        if (metaData.Height == bestHeight) 
                        {
                            // prefer mp4 over webm
                            if(item.Metadata.MimeType.Equals("video/webm")) continue; 

                            // prefer adaptive over normal streams
                            if(item.StreamType == StreamType.VIDEO_AUDIO) continue;
                        }

                        video = item;

                        bestHeight = metaData.Height;
                    }
                }

                if (video.StreamType == StreamType.VIDEO_AUDIO) return;

                // get best matching audio stream
                foreach (YoutubeVideoStreamedItem item in StreamedItem)
                {
                    VideoMetadata metaData = item.Metadata as VideoMetadata;

                    if (metaData.SamplesPerSecond >= bestSamplesPerSecond)
                    {
                        int idx = video.Metadata.MimeType.IndexOf('/');

                        if (!metaData.MimeType.EndsWith(video.Metadata.MimeType.Substring(idx)))
                        {
                            // only use audio stream that matches with the video stream
                            continue;
                        }

                        if (metaData.SamplesPerSecond == bestSamplesPerSecond)
                        {
                            // prefer aac over ogg
                            if (item.Metadata.MimeType.Equals("audio/webm")) continue;                           
                        }

                        audio = item;

                        bestSamplesPerSecond = metaData.SamplesPerSecond.Value;
                    }
                }

            }
            finally
            {
                RWLock.ExitReadLock();
            }
        }
       
        public override void readMetadata(MediaViewer.Model.metadata.Metadata.MetadataFactory.ReadOptions options, System.Threading.CancellationToken token)
        {            
            String thumbnailMimeType;

            RWLock.EnterWriteLock();
            try
            {
                ItemState = MediaItemState.LOADING;
                
                YoutubeItemMetadata metaData = new YoutubeItemMetadata();

                metaData.Thumbnail = new MediaViewer.MediaDatabase.Thumbnail(loadThumbnail(out thumbnailMimeType, token));
                metaData.CreationDate = PublishedAt;
                metaData.Title = Title;
                metaData.Description = String.IsNullOrEmpty(Description) ? Title : Description;
             
                NameValueCollection videoInfo;

                IsEmbeddedOnly = !getVideoInfo(VideoId, out videoInfo, token);

                if (!IsEmbeddedOnly)
                {
                    YoutubeVideoStreamedItem video, audio;

                    getBestQualityStreams(out video, out audio);

                    VideoMetadata videoMetadata = video.Metadata as VideoMetadata;
                    VideoMetadata audioMetadata = audio == null ? null : audio.Metadata as VideoMetadata;

                    metaData.Width = videoMetadata.Width;
                    metaData.Height = videoMetadata.Height;
                    metaData.MimeType = videoMetadata.MimeType;
                    metaData.FramesPerSecond = videoMetadata.FramesPerSecond == 0 ? null : new Nullable<double>(videoMetadata.FramesPerSecond);
                                        
                    metaData.SizeBytes = videoMetadata.SizeBytes;

                    if (audioMetadata != null)
                    {
                        metaData.SizeBytes += audioMetadata.SizeBytes;
                    }                    
                }
                              
                long durationSeconds;

                if (long.TryParse(videoInfo["length_seconds"], out durationSeconds))
                {
                    metaData.DurationSeconds = durationSeconds;
                }

                long viewCount;

                if (long.TryParse(videoInfo["view_count"], out viewCount))
                {
                    metaData.ViewCount = viewCount;
                }

                double rating;

                if (double.TryParse(videoInfo["avg_rating"], NumberStyles.Float, new CultureInfo("en-US"), out rating))
                {
                    metaData.Rating = rating;
                }

                metaData.Author = videoInfo["author"];
                                   
                String tags = videoInfo["keywords"];
                if (tags != null)
                {
                    string[] tagNames = tags.Split(new char[] { ',' });

                    foreach (string tagName in tagNames)
                    {
                        if (String.IsNullOrEmpty(tagName) || String.IsNullOrWhiteSpace(tagName)) continue;

                        Tag newTag = new Tag();
                        newTag.Name = tagName;

                        metaData.Tags.Add(newTag);
                    }
                }
                                               
                Metadata = metaData;

                ItemState = MediaItemState.LOADED;
            }
            catch (Exception e)
            {
                if (e is System.Net.WebException &&
                    ((System.Net.WebException)e).Status == WebExceptionStatus.Timeout)
                {
                    ItemState = MediaItemState.TIMED_OUT;
                }
                else
                {
                    ItemState = MediaItemState.ERROR;
                }
            }
            finally
            {
                RWLock.ExitWriteLock();
            }
        }

        bool getVideoInfo(String videoId, out NameValueCollection videoInfo, CancellationToken token)
        {                   
            Uri location = new Uri("http://www.youtube.com/get_video_info?video_id=" + videoId + "&ps=default&eurl=&gl=US&hl=en");

            videoInfo = getInfo(location, token);
            
            if (videoInfo["status"].Equals("fail"))
            {
                return (false);
            }

            String streamMapsEncoded = videoInfo["url_encoded_fmt_stream_map"];

            String[] streamMapEncoded = streamMapsEncoded.Split(new char[] { ',' });

            foreach (String map in streamMapEncoded)
            {
                NameValueCollection mapArgsEnc = (HttpUtility.ParseQueryString(map));
                NameValueCollection mapArgsDec = new NameValueCollection();

                foreach (String key in mapArgsEnc)
                {
                    mapArgsDec.Add(key, HttpUtility.UrlDecode(mapArgsEnc[key]));
                }

                StreamedItem.Add(new YoutubeVideoStreamedItem(mapArgsDec, Name));
            }

            streamMapsEncoded = videoInfo["adaptive_fmts"];

            if (streamMapsEncoded == null) return(true);

            streamMapEncoded = streamMapsEncoded.Split(new char[] { ',' });

            foreach (String map in streamMapEncoded)
            {
                NameValueCollection mapArgsEnc = (HttpUtility.ParseQueryString(map));
                NameValueCollection mapArgsDec = new NameValueCollection();

                foreach (String key in mapArgsEnc)
                {
                    mapArgsDec.Add(key, HttpUtility.UrlDecode(mapArgsEnc[key]));
                }

                StreamedItem.Add(new YoutubeVideoStreamedItem(mapArgsDec, Name));                
            }

            return (true);
        }

        public String VideoId
        {
            get
            {
                if (Info is SearchResult)
                {
                    return (Info as SearchResult).Id.VideoId;
                }
                else if (Info is PlaylistItem)
                {
                    return (Info as PlaylistItem).Snippet.ResourceId.VideoId;
                }

                return (null);
            }
        }

        public String Title
        {
            get
            {
                if (Info is SearchResult)
                {
                    return (Info as SearchResult).Snippet.Title;
                }
                else if (Info is PlaylistItem)
                {
                    return (Info as PlaylistItem).Snippet.Title;
                }

                return (null);
            }
        }

        public String Description
        {
            get
            {
                if (Info is SearchResult)
                {
                    return (Info as SearchResult).Snippet.Description;
                }
                else if (Info is PlaylistItem)
                {
                    return (Info as PlaylistItem).Snippet.Description;
                }

                return (null);
            }
        }

        public DateTime? PublishedAt
        {
            get
            {
                if (Info is SearchResult)
                {
                    return (Info as SearchResult).Snippet.PublishedAt;
                }
                else if (Info is PlaylistItem)
                {
                    return (Info as PlaylistItem).Snippet.PublishedAt;
                }

                return (null);
            }
        }

        

    }
}
