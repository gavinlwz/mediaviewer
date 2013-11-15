﻿using MediaViewer.ImageGrid;
using MediaViewer.MediaFileModel;
using MediaViewer.MediaFileModel.Watcher;
using MvvmFoundation.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaViewer.MetaData
{
    class MetaDataViewModel : ObservableObject
    {

        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        void clear()
        {
            Filename = "";
            Rating = 0;
            Title = "";
            Description = "";
            Author = "";
            Copyright = "";
            dynamicProperties = new List<Tuple<string, string>>();

            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {             
                Tags.Clear();
                AddTags.Clear();
                RemoveTags.Clear();
            }));
        }

        public MetaDataViewModel()
        {
            Tags = new ObservableCollection<string>();
            AddTags = new ObservableCollection<string>();
            RemoveTags = new ObservableCollection<string>();
            clear();
            BatchMode = false;
            IsEnabled = false;
            
            writeMetaDataCommand = new Command(new Action(async () =>
            {
                MetaDataUpdateView metaDataUpdateView = new MetaDataUpdateView();
                metaDataUpdateView.Show();
                MetaDataUpdateViewModel vm = (MetaDataUpdateViewModel)metaDataUpdateView.DataContext;
                await vm.writeMetaData(new MetaDataUpdateViewModelAsyncState(this));

            }));

            MediaFileWatcher.Instance.MediaFiles.ItemIsSelectedChanged += new EventHandler((s,e) =>
            {
                ItemList = MediaFileWatcher.Instance.MediaFiles.GetSelectedItems();
            });
        }

        List<MediaFileItem> itemList;

        public List<MediaFileItem> ItemList
        {
            get { return itemList; }
            set
            {
                itemList = value;

                grabData();
                NotifyPropertyChanged();
            }
        }

        Command writeMetaDataCommand;

        public Command WriteMetaDataCommand
        {
            get { return writeMetaDataCommand; }
            set { writeMetaDataCommand = value; }
        }


        string filename;

        public string Filename
        {
            get { return filename; }
            set
            {
                filename = value;
                NotifyPropertyChanged();
            }
        }

        bool filenameEnabled;

        public bool FilenameEnabled
        {
            get { return filenameEnabled; }
            set
            {
                filenameEnabled = value;
                NotifyPropertyChanged();
            }
        }

        float rating;

        public float Rating
        {
            get { return rating; }
            set
            {
                rating = value;
                NotifyPropertyChanged();
            }
        }

        bool ratingEnabled;

        public bool RatingEnabled
        {
            get { return ratingEnabled; }
            set
            {
                ratingEnabled = value;
                NotifyPropertyChanged();
            }
        }

        string title;

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                NotifyPropertyChanged();
            }
        }

        bool titleEnabled;

        public bool TitleEnabled
        {
            get { return titleEnabled; }
            set
            {
                titleEnabled = value;
                NotifyPropertyChanged();
            }
        }

        string description;

        public string Description
        {
            get { return description; }
            set
            {
                description = value;
                NotifyPropertyChanged();
            }
        }

        bool descriptionEnabled;

        public bool DescriptionEnabled
        {
            get { return descriptionEnabled; }
            set
            {
                descriptionEnabled = value;
                NotifyPropertyChanged();
            }
        }

        string author;

        public string Author
        {
            get { return author; }
            set
            {
                author = value;
                NotifyPropertyChanged();
            }
        }

        bool authorEnabled;

        public bool AuthorEnabled
        {
            get { return authorEnabled; }
            set
            {
                authorEnabled = value;
                NotifyPropertyChanged();
            }
        }


        string copyright;

        public string Copyright
        {
            get { return copyright; }

            set
            {
                copyright = value;
                NotifyPropertyChanged();
            }
        }

        bool copyrightEnabled;

        public bool CopyrightEnabled
        {
            get { return copyrightEnabled; }
            set
            {
                copyrightEnabled = value;
                NotifyPropertyChanged();
            }
        }

        ObservableCollection<String> tags;

        public ObservableCollection<String> Tags
        {
            get { return tags; }
            set { tags = value;
            NotifyPropertyChanged();
            }
        }

        ObservableCollection<String> addTags;

        public ObservableCollection<String> AddTags
        {
            get { return addTags; }
            set { addTags = value;
            NotifyPropertyChanged();
            }
        }

        ObservableCollection<String> removeTags;

        public ObservableCollection<String> RemoveTags
        {
            get { return removeTags; }
            set { removeTags = value;
            NotifyPropertyChanged();
            }
        }

        bool isEnabled;

        public bool IsEnabled
        {
            get { return isEnabled; }
            set
            {
                isEnabled = value;
                NotifyPropertyChanged();
                if (isEnabled == false)
                {
                    FilenameEnabled = false;
                    RatingEnabled = false;
                    TitleEnabled = false;
                    DescriptionEnabled = false;
                    AuthorEnabled = false;
                    CopyrightEnabled = false;

                }
            }
        }


        bool batchMode;

        public bool BatchMode
        {
            get { return batchMode; }
            set
            {
                batchMode = value;
                NotifyPropertyChanged();

                if (batchMode == true && IsEnabled == true)
                {
                    FilenameEnabled = false;
                    RatingEnabled = false;
                    TitleEnabled = false;
                    DescriptionEnabled = false;
                    AuthorEnabled = false;
                    CopyrightEnabled = false;
                }
                else if(IsEnabled == true)
                {
                    FilenameEnabled = true;
                    RatingEnabled = true;
                    TitleEnabled = true;
                    DescriptionEnabled = true;
                    AuthorEnabled = true;
                    CopyrightEnabled = true;
                }

            }
        }

        List<Tuple<String, String>> dynamicProperties;

        public List<Tuple<String, String>> DynamicProperties
        {
            get { return dynamicProperties; }
        }

      

        void grabData()
        {
            

            if (itemList.Count == 1 && ItemList[0].Media != null)
            {
                MediaFile media = ItemList[0].Media;

                Filename = Path.GetFileNameWithoutExtension(media.Name);

                FileMetaData metaData = media.MetaData;

                if (media is VideoFile)
                {

                    dynamicProperties = getVideoProperties(media as VideoFile);
                }

                if (metaData != null)
                {
                    Rating = metaData.Rating / 5;
                    Title = metaData.Title;
                    Description = metaData.Description;
                    Author = metaData.Creator;
                    Copyright = metaData.Copyright;
                   
                    foreach (string tag in metaData.Tags)
                    {
                        Tags.Add(tag);
                    }

                    dynamicProperties.AddRange(FormatMetaData.formatProperties(metaData.MiscProps));

                    if (metaData.CreationDate != DateTime.MinValue)
                    {
                        dynamicProperties.Add(new Tuple<string, string>("Creation", metaData.CreationDate.ToString("R")));                       
                    }

                    if (metaData.ModifiedDate != DateTime.MinValue)
                    {
                        dynamicProperties.Add(new Tuple<string, string>("Modified", metaData.ModifiedDate.ToString("R")));
                    }

                    if (metaData.MetaDataDate != DateTime.MinValue)
                    {
                        dynamicProperties.Add(new Tuple<string, string>("Metadata", metaData.MetaDataDate.ToString("R")));
                    }
                    
                }

                IsEnabled = true;
                BatchMode = false;

            }
            else if (itemList.Count > 1 && BatchMode == true)
            {

            }
            else
            {
                if (itemList.Count > 1)
                {
                    IsEnabled = true;
                    BatchMode = true;                   
                
                } else if(itemList.Count == 0) {

                    BatchMode = false;
                    IsEnabled = false;
                    
                }

                clear();
            }


        }

        List<Tuple<String, String>> getVideoProperties(VideoFile video)
        {
            List<Tuple<String, String>> p = new List<Tuple<string, string>>();

            p.Add(new Tuple<string, string>("", "VIDEO"));
            p.Add(new Tuple<string, string>("Video Container", video.Container));
            p.Add(new Tuple<string, string>("Video Codec", video.VideoCodecName));
            p.Add(new Tuple<string, string>("Resolution", video.Width.ToString() + " x " + video.Height.ToString()));
            p.Add(new Tuple<string, string>("Duration", Utils.Misc.formatTimeSeconds(video.DurationSeconds)));
            p.Add(new Tuple<string, string>("Pixel Format", video.PixelFormat));
            p.Add(new Tuple<string, string>("Frames Per Second", video.FrameRate.ToString()));

            if (video.HasAudio)
            {
                p.Add(new Tuple<string, string>("Audio Codec", video.AudioCodecName));
                p.Add(new Tuple<string, string>("Bits Per Sample", (video.BytesPerSample * 8).ToString()));
                p.Add(new Tuple<string, string>("Samples Per Second", video.SamplesPerSecond.ToString()));
                p.Add(new Tuple<string, string>("Nr Channels", video.NrChannels.ToString()));
            }

            return (p);

        }
        
    }
}
