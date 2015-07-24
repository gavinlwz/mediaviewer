﻿using MediaViewer.MediaDatabase;
using MediaViewer.Model.Media.File;
using MediaViewer.Model.Media.State.CollectionView;
using MediaViewer.Model.Utils;
using MediaViewer.Model.Utils.Strings;
using MediaViewer.UserControls.SortComboBox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.Model.Media.File
{
    public enum MediaFileSortMode
    {
        Name,
        Size,
        Rating,
        Imported,
        Tags,
        MimeType,
        CreationDate,
        FileDate,
        LastModified,
        SoftWare,
        Width,
        Height,
        // video
        Duration,
        FramesPerSecond,
        VideoCodec,
        AudioCodec,
        PixelFormat,
        BitsPerSample,
        SamplesPerSecond,
        NrChannels,
        // image
        CameraMake,
        CameraModel,
        Lens,
        FocalLength,
        ExposureTime,
        FNumber,
        ISOSpeedRating
    }

    public class MediaFileSortItem : SortItemBase<MediaFileSortMode> {

        public MediaFileSortItem(MediaFileSortMode mode) 
            : base(mode)
        {
        }
    }
   
    class MediaFileSortFunctions
    {
        public static Func<SelectableMediaItem, SelectableMediaItem, int> getSortFunction(MediaFileSortMode sortMode)
        {
            Func<SelectableMediaItem, SelectableMediaItem, int> sortFunc = null;

            switch (sortMode)
            {
                case MediaFileSortMode.Name:

                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {                                                     
                            int result = NaturalSortOrder.CompareNatural(Path.GetFileName(a.Item.Location),
                                Path.GetFileName(b.Item.Location));
     
                            return (result);
                        });
                    break;
                case MediaFileSortMode.Size:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            return(onEquals(a.Item.Metadata.SizeBytes.CompareTo(b.Item.Metadata.SizeBytes), a, b));
                       
                        });
                    break;
                case MediaFileSortMode.Rating:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;
                 
                            return (onEquals(Nullable.Compare(a.Item.Metadata.Rating, b.Item.Metadata.Rating), a, b));

                        });
                    break;
                case MediaFileSortMode.Imported:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;
           
                            return (onEquals(a.Item.Metadata.IsImported.CompareTo(b.Item.Metadata.IsImported), a, b));
                        });
                    break;
                case MediaFileSortMode.Tags:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            return (onEquals(a.Item.Metadata.Tags.Count.CompareTo(b.Item.Metadata.Tags.Count),a,b));

                        });
                    break;
                case MediaFileSortMode.FileDate:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                       (a, b) =>
                       {
                           int result = hasMediaTest(a, b);
                           if (result != 0) return result;

                           return (onEquals(a.Item.Metadata.FileDate.CompareTo(b.Item.Metadata.FileDate),a,b));
                       });
                    break;
                case MediaFileSortMode.MimeType:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            return (onEquals(a.Item.Metadata.MimeType.CompareTo(b.Item.Metadata.MimeType),a,b));
                        });
                    break;
                case MediaFileSortMode.LastModified:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            return (onEquals(a.Item.Metadata.LastModifiedDate.CompareTo(b.Item.Metadata.LastModifiedDate),a,b));
                        });
                    break;
                case MediaFileSortMode.CreationDate:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            return (onEquals(Nullable.Compare(a.Item.Metadata.CreationDate, b.Item.Metadata.CreationDate),a,b));
                        });
                    break;
                case MediaFileSortMode.SoftWare:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            return (onEquals(Compare(a.Item.Metadata.Software, b.Item.Metadata.Software),a,b));
                        });
                    break;
                case MediaFileSortMode.Width:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            int widthA;
                            int widthB;

                            if (a.Item.Metadata is ImageMetadata)
                            {
                                widthA = (a.Item.Metadata as ImageMetadata).Width;
                            }
                            else
                            {
                                widthA = (a.Item.Metadata as VideoMetadata).Width;
                            }

                            if (b.Item.Metadata is ImageMetadata)
                            {
                                widthB = (b.Item.Metadata as ImageMetadata).Width;
                            }
                            else
                            {
                                widthB = (b.Item.Metadata as VideoMetadata).Width;
                            }

                            return (onEquals(widthA.CompareTo(widthB),a,b));
                        });
                    break;
                case MediaFileSortMode.Height:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            int heightA;
                            int heightB;

                            if (a.Item.Metadata is ImageMetadata)
                            {
                                heightA = (a.Item.Metadata as ImageMetadata).Height;
                            }
                            else
                            {
                                heightA = (a.Item.Metadata as VideoMetadata).Height;
                            }

                            if (b.Item.Metadata is ImageMetadata)
                            {
                                heightB = (b.Item.Metadata as ImageMetadata).Height;
                            }
                            else
                            {
                                heightB = (b.Item.Metadata as VideoMetadata).Height;
                            }

                            return (onEquals(heightA.CompareTo(heightB),a,b));
                        });
                    break;
                case MediaFileSortMode.Duration:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            Nullable<int> aDuration = a.Item.Metadata is VideoMetadata ? new Nullable<int>((a.Item.Metadata as VideoMetadata).DurationSeconds) : null;
                            Nullable<int> bDuration = b.Item.Metadata is VideoMetadata ? new Nullable<int>((b.Item.Metadata as VideoMetadata).DurationSeconds) : null;

                            return (onEquals(Nullable.Compare<int>(aDuration, bDuration),a,b));
                        });
                    break;
                case MediaFileSortMode.FramesPerSecond:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            Nullable<double> aFPS = a.Item.Metadata is VideoMetadata ? new Nullable<double>((a.Item.Metadata as VideoMetadata).FramesPerSecond) : null;
                            Nullable<double> bFPS = b.Item.Metadata is VideoMetadata ? new Nullable<double>((b.Item.Metadata as VideoMetadata).FramesPerSecond) : null;

                            return (onEquals(Nullable.Compare<double>(aFPS, bFPS),a,b));
                        });
                    break;
                case MediaFileSortMode.VideoCodec:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            String aVideoCodec = a.Item.Metadata is VideoMetadata ? (a.Item.Metadata as VideoMetadata).VideoCodec : "";
                            String bVideoCodec = b.Item.Metadata is VideoMetadata ? (b.Item.Metadata as VideoMetadata).VideoCodec : "";

                            return (onEquals(aVideoCodec.CompareTo(bVideoCodec),a,b));
                        });
                    break;
                case MediaFileSortMode.AudioCodec:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            String aAudioCodec = a.Item.Metadata is VideoMetadata ? (a.Item.Metadata as VideoMetadata).AudioCodec : null;
                            String bAudioCodec = b.Item.Metadata is VideoMetadata ? (b.Item.Metadata as VideoMetadata).AudioCodec : null;

                            return (onEquals(Compare(aAudioCodec, bAudioCodec),a,b));
                        });
                    break;
                case MediaFileSortMode.PixelFormat:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            String aPixelFormat = a.Item.Metadata is VideoMetadata ? (a.Item.Metadata as VideoMetadata).PixelFormat : null;
                            String bPixelFormat = b.Item.Metadata is VideoMetadata ? (b.Item.Metadata as VideoMetadata).PixelFormat : null;

                            return (onEquals(Compare(aPixelFormat, bPixelFormat),a,b));
                        });
                    break;
                case MediaFileSortMode.BitsPerSample:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            Nullable<short> aBPS = a.Item.Metadata is VideoMetadata ? (a.Item.Metadata as VideoMetadata).BitsPerSample : null;
                            Nullable<short> bBPS = b.Item.Metadata is VideoMetadata ? (b.Item.Metadata as VideoMetadata).BitsPerSample : null;

                            return (onEquals(Nullable.Compare(aBPS, bBPS),a,b));
                        });
                    break;
                case MediaFileSortMode.SamplesPerSecond:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            Nullable<int> aSPS = a.Item.Metadata is VideoMetadata ? (a.Item.Metadata as VideoMetadata).SamplesPerSecond : null;
                            Nullable<int> bSPS = b.Item.Metadata is VideoMetadata ? (b.Item.Metadata as VideoMetadata).SamplesPerSecond : null;

                            return (onEquals(Nullable.Compare(aSPS, bSPS),a,b));
                        });
                    break;
                case MediaFileSortMode.NrChannels:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            Nullable<int> aNrChannels = a.Item.Metadata is VideoMetadata ? (a.Item.Metadata as VideoMetadata).NrChannels : null;
                            Nullable<int> bNrChannels = b.Item.Metadata is VideoMetadata ? (b.Item.Metadata as VideoMetadata).NrChannels : null;

                            return (onEquals(Nullable.Compare(aNrChannels, bNrChannels),a,b));
                        });
                    break;
                case MediaFileSortMode.CameraMake:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            String aVal = a.Item.Metadata is ImageMetadata ? (a.Item.Metadata as ImageMetadata).CameraMake : null;
                            String bVal = b.Item.Metadata is ImageMetadata ? (b.Item.Metadata as ImageMetadata).CameraMake : null;

                            return (onEquals(Compare(aVal, bVal),a,b));
                        });
                    break;
                case MediaFileSortMode.CameraModel:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            String aVal = a.Item.Metadata is ImageMetadata ? (a.Item.Metadata as ImageMetadata).CameraModel : null;
                            String bVal = b.Item.Metadata is ImageMetadata ? (b.Item.Metadata as ImageMetadata).CameraModel : null;

                            return (onEquals(Compare(aVal, bVal),a,b));
                        });
                    break;
                case MediaFileSortMode.Lens:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            String aVal = a.Item.Metadata is ImageMetadata ? (a.Item.Metadata as ImageMetadata).Lens : null;
                            String bVal = b.Item.Metadata is ImageMetadata ? (b.Item.Metadata as ImageMetadata).Lens : null;

                            return (onEquals(Compare(aVal, bVal),a,b));
                        });
                    break;
                case MediaFileSortMode.FocalLength:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            Nullable<double> aVal = a.Item.Metadata is ImageMetadata ? (a.Item.Metadata as ImageMetadata).FocalLength : null;
                            Nullable<double> bVal = b.Item.Metadata is ImageMetadata ? (b.Item.Metadata as ImageMetadata).FocalLength : null;

                            return (onEquals(Nullable.Compare(aVal, bVal),a,b));
                        });
                    break;
                case MediaFileSortMode.FNumber:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            Nullable<double> aVal = a.Item.Metadata is ImageMetadata ? (a.Item.Metadata as ImageMetadata).FNumber : null;
                            Nullable<double> bVal = b.Item.Metadata is ImageMetadata ? (b.Item.Metadata as ImageMetadata).FNumber : null;

                            return (onEquals(Nullable.Compare(aVal, bVal),a,b));
                        });
                    break;
                case MediaFileSortMode.ExposureTime:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            Nullable<double> aVal = a.Item.Metadata is ImageMetadata ? (a.Item.Metadata as ImageMetadata).ExposureTime : null;
                            Nullable<double> bVal = b.Item.Metadata is ImageMetadata ? (b.Item.Metadata as ImageMetadata).ExposureTime : null;

                            return (onEquals(Nullable.Compare(aVal, bVal),a,b));
                        });
                    break;
                case MediaFileSortMode.ISOSpeedRating:
                    sortFunc = new Func<SelectableMediaItem, SelectableMediaItem, int>(
                        (a, b) =>
                        {
                            int result = hasMediaTest(a, b);
                            if (result != 0) return result;

                            Nullable<int> aVal = a.Item.Metadata is ImageMetadata ? (a.Item.Metadata as ImageMetadata).ISOSpeedRating : null;
                            Nullable<int> bVal = b.Item.Metadata is ImageMetadata ? (b.Item.Metadata as ImageMetadata).ISOSpeedRating : null;

                            return (onEquals(Nullable.Compare(aVal, bVal),a,b));
                        });
                    break;
                default:
                    break;
            }

            return (sortFunc);
        }

        public static bool isAllSortMode(MediaFileSortMode mode)
        {
            if ((int)mode <= (int)MediaFileSortMode.SoftWare) return (true);
            else return (false);
        }

        public static bool isVideoSortMode(MediaFileSortMode mode)
        {
            if ((int)mode <= (int)MediaFileSortMode.NrChannels) return (true);
            else return (false);
        }

        public static bool isImageSortMode(MediaFileSortMode mode)
        {
            if ((int)mode <= (int)MediaFileSortMode.Height || (int)mode >= (int)MediaFileSortMode.CameraMake) return (true);
            else return (false);
        }

        static int onEquals(int result, SelectableMediaItem a, SelectableMediaItem b)
        {
            if (result == 0)
            {
                return (System.IO.Path.GetFileName(a.Item.Location).CompareTo(System.IO.Path.GetFileName(b.Item.Location)));
            }
            else
            {
                return (result);
            }
        }

        static int hasMediaTest(SelectableMediaItem a, SelectableMediaItem b)
        {
            if (a.Item.Metadata != null && b.Item.Metadata != null) return 0;
            if (a.Item.Metadata == null) return 1;
            else return -1;
        }

        static int Compare<T>(T a, T b) where T : IComparable
        {
            if (a == null) return -1;
            if (b == null) return 1;
            return (a.CompareTo(b));
        }
    }
}
