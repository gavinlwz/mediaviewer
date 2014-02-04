﻿using MediaViewer.DirectoryPicker;
using MediaViewer.ImageGrid;
using MediaViewer.MediaDatabase;
using MediaViewer.MediaDatabase.DbCommands;
using MediaViewer.MediaFileModel;
using MediaViewer.MediaFileModel.Watcher;
using MediaViewer.Utils;
using MvvmFoundation.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MediaViewer.MetaData
{
    class MetaDataViewModel : ObservableObject
    {

        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        void clear()
        {
            Filename = "";
            Location = "";
            
            Rating = 0;
            Title = "";
            Description = "";
            Author = "";
            Copyright = "";
            Creation = DateTime.MinValue;
            dynamicProperties = new List<Tuple<string, string>>();

            SelectedMetaDataPreset = noPresetMetaData;

            lock (tagsLock)
            {
                Tags.Clear();
            }
            lock (addTagsLock)
            {
                AddTags.Clear();
            }
            lock (removeTagsLock)
            {
                RemoveTags.Clear();
            }
            
        }

        public MetaDataViewModel()
        {
            Tags = new ObservableCollection<Tag>();
            tagsLock = new Object();
            BindingOperations.EnableCollectionSynchronization(Tags, tagsLock);

            AddTags = new ObservableCollection<Tag>();
            addTagsLock = new Object();
            BindingOperations.EnableCollectionSynchronization(AddTags, addTagsLock);

            RemoveTags = new ObservableCollection<Tag>();
            removeTagsLock = new Object();
            BindingOperations.EnableCollectionSynchronization(RemoveTags, removeTagsLock);

            MetaDataPresets = new ObservableCollection<PresetMetadata>();

            loadMetaDataPresets();

            clear();
            BatchMode = false;
            IsEnabled = false;
            
            writeMetaDataCommand = new Command(new Action(async () =>
            {
                MetaDataUpdateView metaDataUpdateView = new MetaDataUpdateView();
                metaDataUpdateView.Show();
                MetaDataUpdateViewModel vm = (MetaDataUpdateViewModel)metaDataUpdateView.DataContext;
                await vm.writeMetaDataAsync(new MetaDataUpdateViewModelAsyncState(this));

            }));

            filenamePresetsCommand = new Command(new Action(() =>
            {
                FilenamePresetsView filenamePreset = new FilenamePresetsView();
                FilenamePresetsViewModel vm = (FilenamePresetsViewModel)filenamePreset.DataContext;

                if (filenamePreset.ShowDialog() == true)
                {
                    Filename = vm.SelectedPreset;                    
                }

            })); 

            directoryPickerCommand = new Command(new Action(() => 
            {
                DirectoryPickerView directoryPicker = new DirectoryPickerView();
                DirectoryPickerViewModel vm = (DirectoryPickerViewModel)directoryPicker.DataContext;
                vm.MovePath = String.IsNullOrEmpty(Location) ? MediaFileWatcher.Instance.Path : Location;
                vm.SelectedItems = ItemList;
                vm.MovePathHistory = Settings.AppSettings.Instance.MetaDataUpdateDirectoryHistory;
              
                if (directoryPicker.ShowDialog() == true)
                {                    
                    Location = vm.MovePath;                    
                }

            }));

            metaDataPresetsCommand = new Command(new Action(() =>
                {
                    MetaDataPresetsView metaDataPresets = new MetaDataPresetsView();
                    metaDataPresets.ShowDialog();
                    loadMetaDataPresets();                    

                }));

            insertCounterCommand = new Command<int>(new Action<int>((startIndex) =>
            {
                try
                {                    
                    Filename = Filename.Insert(startIndex, "\"" + MetaDataUpdateViewModel.counterMarker + 
                        MetaDataUpdateViewModel.defaultCounter + "\"");
                }
                catch (Exception e)
                {
                    log.Error(e);
                }

            }));

            insertExistingFilenameCommand = new Command<int>(new Action<int>((startIndex) =>
                {
                    try
                    {
                        Filename = Filename.Insert(startIndex, "\"" + MetaDataUpdateViewModel.oldFilenameMarker + "\"");
                    }
                    catch (Exception e)
                    {
                        log.Error(e);
                    }

                }));

            insertResolutionCommand = new Command<int>(new Action<int>((startIndex) =>
            {
                try
                {
                    Filename = Filename.Insert(startIndex, "\"" + MetaDataUpdateViewModel.resolutionMarker + "\"");
                }
                catch (Exception e)
                {
                    log.Error(e);
                }

            }));

            insertDateCommand = new Command<int>(new Action<int>((startIndex) =>
            {
                try
                {
                    Filename = Filename.Insert(startIndex, "\"" + MetaDataUpdateViewModel.dateMarker 
                        + MetaDataUpdateViewModel.defaultDateFormat + "\"");
                }
                catch (Exception e)
                {
                    log.Error(e);
                }

            }));

            MediaFileWatcher.Instance.MediaState.ItemIsSelectedChanged += new EventHandler((s,e) =>
            {
                ItemList = MediaFileWatcher.Instance.MediaState.getSelectedItems();
            });

            FilenameHistory = Settings.AppSettings.Instance.FilenameHistory;           

            MovePathHistory = Settings.AppSettings.Instance.MetaDataUpdateDirectoryHistory;
            
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

        Command<int> insertCounterCommand;

        public Command<int> InsertCounterCommand
        {
            get { return insertCounterCommand; }
            set { insertCounterCommand = value; }
        }

        Command<int> insertExistingFilenameCommand;

        public Command<int> InsertExistingFilenameCommand
        {
            get { return insertExistingFilenameCommand; }
            set { insertExistingFilenameCommand = value; }
        }

        Command<int> insertResolutionCommand;

        public Command<int> InsertResolutionCommand
        {
            get { return insertResolutionCommand; }
            set { insertResolutionCommand = value; }
        }

        Command<int> insertDateCommand;

        public Command<int> InsertDateCommand
        {
            get { return insertDateCommand; }
            set { insertDateCommand = value; }
        }

        String location;

        public String Location
        {
            get { return location; }
            set
            {           
                location = value;
                NotifyPropertyChanged();
            }
        }

        Command filenamePresetsCommand;

        public Command FilenamePresetsCommand
        {
            get { return filenamePresetsCommand; }
            set { filenamePresetsCommand = value; }
        }

        Command directoryPickerCommand;

        public Command DirectoryPickerCommand
        {
            get { return directoryPickerCommand; }
            set { directoryPickerCommand = value; }
        }

        Command metaDataPresetsCommand;

        public Command MetaDataPresetsCommand
        {
            get { return metaDataPresetsCommand; }
            set { metaDataPresetsCommand = value; }
        }

        static PresetMetadata noPresetMetaData = new PresetMetadata() { Name = "None", Id = -1 };

        ObservableCollection<PresetMetadata> metaDataPresets;

        public ObservableCollection<PresetMetadata> MetaDataPresets
        {
            get { return metaDataPresets; }
            set { metaDataPresets = value; }
        }

        PresetMetadata selectedMetaDataPreset;

        public PresetMetadata SelectedMetaDataPreset
        {
            get { return selectedMetaDataPreset; }
            set
            {
                bool dontGrabData = false;

                if (value != null && selectedMetaDataPreset != null)
                {
                    if (value.Id == -1 && selectedMetaDataPreset.Id == -1)
                    {
                        dontGrabData = true;
                    }
                }

                selectedMetaDataPreset = value;
               
                if (selectedMetaDataPreset== null)
                {

                }
                else if (selectedMetaDataPreset.Id == -1)
                {
                    if (itemList != null && itemList.Count == 1 && dontGrabData == false)
                    {
                        grabData();
                    }
                }
                else
                {
                    selectedMetaDataPreset = value;

                    if (selectedMetaDataPreset.IsTitleEnabled)
                    {
                        TitleEnabled = true;
                        Title = selectedMetaDataPreset.Title;
                    }
                    if (selectedMetaDataPreset.IsRatingEnabled)
                    {
                        RatingEnabled = true;
                        Rating = (float)selectedMetaDataPreset.Rating;
                    }
                    if (selectedMetaDataPreset.IsDescriptionEnabled)
                    {
                        DescriptionEnabled = true;
                        Description = selectedMetaDataPreset.Description;
                    }
                    if (selectedMetaDataPreset.IsAuthorEnabled)
                    {
                        AuthorEnabled = true;
                        Author = selectedMetaDataPreset.Author;
                    }

                    if (selectedMetaDataPreset.IsCreationDateEnabled)
                    {
                        CreationEnabled = true;
                        Creation = selectedMetaDataPreset.CreationDate;
                    }

                    if (selectedMetaDataPreset.IsCopyrightEnabled)
                    {
                        CopyrightEnabled = true;
                        Copyright = selectedMetaDataPreset.Copyright;
                    }

                }
                NotifyPropertyChanged();
            }

        }

        void loadMetaDataPresets()
        {           
            MetaDataPresets.Clear();

            MetaDataPresets.Add(noPresetMetaData);

            using (PresetMetadataDbCommands db = new PresetMetadataDbCommands())
            {
                foreach (PresetMetadata data in db.getAllPresets())
                {
                    MetaDataPresets.Add(data);
                }

            }
            SelectedMetaDataPreset = noPresetMetaData;
        }

        Nullable<double> rating;

        public Nullable<double> Rating
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

        DateTime creation;

        public DateTime Creation
        {
            get { return creation; }
            set { creation = value;
            NotifyPropertyChanged();
            }
        }

        bool creationEnabled;

        public bool CreationEnabled
        {
            get { return creationEnabled; }
            set { creationEnabled = value;
            NotifyPropertyChanged();
            }
        }

        Object tagsLock;
        ObservableCollection<Tag> tags;

        public ObservableCollection<Tag> Tags
        {
            get { return tags; }
            set { tags = value;
            NotifyPropertyChanged();
            }
        }

        Object addTagsLock;
        ObservableCollection<Tag> addTags;

        public ObservableCollection<Tag> AddTags
        {
            get { return addTags; }
            set { addTags = value;
            NotifyPropertyChanged();
            }
        }

        Object removeTagsLock;
        ObservableCollection<Tag> removeTags;

        public ObservableCollection<Tag> RemoveTags
        {
            get { return removeTags; }
            set { removeTags = value;
            NotifyPropertyChanged();
            }
        }

        ObservableCollection<String> filenameHistory;

        public ObservableCollection<String> FilenameHistory
        {
            get { return filenameHistory; }
            set { filenameHistory = value; }
        }

        ObservableCollection<String> movePathHistory;

        public ObservableCollection<String> MovePathHistory
        {
            get { return movePathHistory; }
            set { movePathHistory = value; }
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
                    RatingEnabled = false;
                    TitleEnabled = false;
                    DescriptionEnabled = false;
                    AuthorEnabled = false;
                    CopyrightEnabled = false;
                    CreationEnabled = false;

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
                    RatingEnabled = false;
                    TitleEnabled = false;
                    DescriptionEnabled = false;
                    AuthorEnabled = false;
                    CopyrightEnabled = false;
                    CreationEnabled = false;
                }
                else if(IsEnabled == true)
                {               
                    RatingEnabled = true;
                    TitleEnabled = true;
                    DescriptionEnabled = true;
                    AuthorEnabled = true;
                    CopyrightEnabled = true;
                    CreationEnabled = true;
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
                
                Media media = ItemList[0].Media;
                             
                if (media.SupportsXMPMetadata == false)
                {
                    clear();
                }

                Filename = Path.GetFileNameWithoutExtension(media.Location);
                Location = FileUtils.getPathWithoutFileName(media.Location);

                if (media is VideoMedia)
                {
                    dynamicProperties = getVideoProperties(media as VideoMedia);
                }

                Rating = media.Rating == null ? null : new Nullable<double>(media.Rating.Value / 5);
                Title = media.Title;
                Description = media.Description;
                Author = media.Author;
                Copyright = media.Copyright;
                Creation = media.CreationDate == null ? DateTime.MinValue : media.CreationDate.Value;

                lock (tagsLock)
                {
                    Tags.Clear();

                    foreach (Tag tag in media.Tags)
                    {
                        Tags.Add(tag);
                    }
                }

                /*dynamicProperties.AddRange(FormatMetaData.formatProperties(metaData.MiscProps));

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
                 */
                
                SelectedMetaDataPreset = noPresetMetaData;
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
                    clear();
                                   
                } else if(itemList.Count == 0) {

                    BatchMode = false;
                    IsEnabled = false;
                    clear();
                }

                              
            }


        }

    

        List<Tuple<String, String>> getVideoProperties(VideoMedia video)
        {
            List<Tuple<String, String>> p = new List<Tuple<string, string>>();

            p.Add(new Tuple<string, string>("", "VIDEO"));
            p.Add(new Tuple<string, string>("Video Container", video.VideoContainer));
            p.Add(new Tuple<string, string>("Video Codec", video.VideoCodec));
            p.Add(new Tuple<string, string>("Resolution", video.Width.ToString() + " x " + video.Height.ToString()));
            p.Add(new Tuple<string, string>("Duration", Utils.Misc.formatTimeSeconds(video.DurationSeconds)));
            p.Add(new Tuple<string, string>("Pixel Format", video.PixelFormat));
            p.Add(new Tuple<string, string>("Frames Per Second", video.FramesPerSecond.ToString()));

            if (video.AudioCodec != null)
            {
                p.Add(new Tuple<string, string>("Audio Codec", video.AudioCodec));
                p.Add(new Tuple<string, string>("Bits Per Sample", video.BitsPerSample.ToString()));
                p.Add(new Tuple<string, string>("Samples Per Second", video.SamplesPerSecond.ToString()));
                p.Add(new Tuple<string, string>("Nr Channels", video.NrChannels.ToString()));
            }

            return (p);

        }
        
    }


}
