﻿using MediaViewer.MediaDatabase;
using MediaViewer.Model.Media.File;
using MediaViewer.Model.Media.File.Watcher;
using MediaViewer.Progress;
using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel.Composition;
using MediaViewer.Model.Settings;
using MediaViewer.Model.Media.State;
using MediaViewer.Model.Utils;
using Microsoft.Practices.Prism.PubSubEvents;
using MediaViewer.Model.Mvvm;
using Microsoft.Practices.Prism.Commands;
using MediaViewer.Infrastructure.Global.Commands;
using MediaViewer.Infrastructure.Logging;
using MediaViewer.Model.Concurrency;
using MediaViewer.Model.metadata.Metadata;

namespace MediaViewer.MetaData
{
    class MetaDataUpdateViewModel : CloseableBindableBase, ICancellableOperationProgress, IDisposable
    {
        AppSettings Settings { get; set; }            
        MediaFileState MediaFileState {get;set;}       
        IEventAggregator EventAggregator { get; set; }

        class Counter
        {
            public Counter(int value, int nrDigits)
            {
                this.value = value;
                this.nrDigits = nrDigits;
            }

            public int value;
            public int nrDigits;
        };
        
        public const string oldFilenameMarker = "filename";
        public const string counterMarker = "counter:";
        public const string replaceMarker = "replace:";
        public const string defaultCounter = "0001";
        public const string resolutionMarker = "resolution";
        public const string dateMarker = "date:";
        public const string defaultDateFormat = "g";

        CancellationTokenSource tokenSource;

        public CancellationTokenSource TokenSource
        {
            get { return tokenSource; }
            set { tokenSource = value; }
        }

        public MetaDataUpdateViewModel(AppSettings settings, MediaFileWatcher mediaFileWatcher, IEventAggregator eventAggregator)
        {
            Settings = settings;
            MediaFileState = mediaFileWatcher.MediaFileState;
            EventAggregator = eventAggregator;

            WindowIcon = "pack://application:,,,/Resources/Icons/info.ico";

            InfoMessages = new ObservableCollection<string>();
            tokenSource = new CancellationTokenSource();
            CancellationToken = tokenSource.Token;

            CancelCommand = new Command(() =>
            {
                TokenSource.Cancel();
            });

            CancelCommand.IsExecutable = true;

            OkCommand = new Command(() =>
            {
                OnClosingRequest();
            });

            OkCommand.IsExecutable = false;
            WindowTitle = "Updating Metadata";          
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool safe)
        {
            if (safe)
            {
                if (tokenSource != null)
                {
                    tokenSource.Dispose();
                    tokenSource = null;
                }
            }
        }

        public async Task writeMetaDataAsync(MetaDataUpdateViewModelAsyncState state)
        {
            TotalProgressMax = state.ItemList.Count;
            ItemProgressMax = 100;
            TotalProgress = 0;
                              
            await Task.Factory.StartNew(() =>
            {
                GlobalCommands.MetaDataUpdateCommand.Execute(state);
                writeMetaData(state);

            }, cancellationToken, TaskCreationOptions.None, PriorityScheduler.BelowNormal);

            OkCommand.IsExecutable = true;
            CancelCommand.IsExecutable = false;
        }

        void writeMetaData(MetaDataUpdateViewModelAsyncState state)
        {

            List<Counter> counters = new List<Counter>();
            String oldPath = "", newPath = "";
            String oldFilename = "", newFilename = "";

            foreach (MediaFileItem item in state.ItemList)
            {              
                if (CancellationToken.IsCancellationRequested) return;

                ItemProgress = 0;
                bool isModified = false;

                ItemInfo = "Opening: " + item.Location;
         
                item.RWLock.EnterUpgradeableReadLock();
                try
                {
                    if (item.Metadata == null)
                    {
                        ItemInfo = "Loading MetaData: " + item.Location;

                        item.readMetadata(MetadataFactory.ReadOptions.AUTO |
                            MetadataFactory.ReadOptions.GENERATE_THUMBNAIL, CancellationToken);

                        if (item.Metadata == null || item.Metadata is UnknownMetadata)
                        {
                            // reload metaData in metadataviewmodel      
                            String error = "Could not open file and/or read it's metadata: " + item.Location;

                            ItemInfo = error;
                            InfoMessages.Add(error);
                            Logger.Log.Error(error);
                        }
                    }


                    if (item.Metadata != null && !(item.Metadata is UnknownMetadata))
                    {
                        isModified = item.Metadata.IsModified;

                        BaseMetadata media = item.Metadata;

                        if (state.RatingEnabled)
                        {
                            Nullable<double> oldValue = media.Rating;

                            media.Rating = state.Rating.HasValue == false ? null : state.Rating * 5;

                            if (media.Rating != oldValue)
                            {
                                isModified = true;
                            }
                        }

                        if (state.TitleEnabled && !EqualityComparer<String>.Default.Equals(media.Title, state.Title))
                        {
                            media.Title = state.Title;
                            isModified = true;
                        }

                        if (state.DescriptionEnabled && !EqualityComparer<String>.Default.Equals(media.Description, state.Description))
                        {
                            media.Description = state.Description;
                            isModified = true;
                        }

                        if (state.AuthorEnabled && !EqualityComparer<String>.Default.Equals(media.Author, state.Author))
                        {
                            media.Author = state.Author;
                            isModified = true;
                        }

                        if (state.CopyrightEnabled && !EqualityComparer<String>.Default.Equals(media.Copyright, state.Copyright))
                        {
                            media.Copyright = state.Copyright;
                            isModified = true;
                        }

                        if (state.CreationEnabled && !(Nullable.Compare<DateTime>(media.CreationDate, state.Creation) == 0))
                        {
                            media.CreationDate = state.Creation;
                            isModified = true;
                        }

                        if(state.IsGeoTagEnabled && !(Nullable.Compare<double>(media.Latitude, state.Latitude) == 0))
                        {
                            media.Latitude = state.Latitude;
                            isModified = true;
                        }

                        if (state.IsGeoTagEnabled && !(Nullable.Compare<double>(media.Longitude, state.Longitude) == 0))
                        {
                            media.Longitude = state.Longitude;
                            isModified = true;
                        }

                        if (state.BatchMode == false && !state.Tags.SequenceEqual(media.Tags))
                        {
                            media.Tags.Clear();
                            foreach (Tag tag in state.Tags)
                            {
                                media.Tags.Add(tag);
                            }
                            isModified = true;
                        }
                        else if (state.BatchMode == true)
                        {
                            bool addedTag = false;
                            bool removedTag = false;

                            foreach (Tag tag in state.AddTags)
                            {
                                // Hashset compares items using their gethashcode function
                                // which can be different for the same database entities created at different times
                                if (!media.Tags.Contains(tag, EqualityComparer<Tag>.Default))
                                {
                                    media.Tags.Add(tag);
                                    addedTag = true;
                                }
                            }

                            foreach (Tag tag in state.RemoveTags)
                            {
                                Tag removeTag = media.Tags.FirstOrDefault((t) => t.Name.Equals(tag.Name));

                                if (removeTag != null)
                                {
                                    media.Tags.Remove(removeTag);
                                    removedTag = true;
                                }

                            }

                            if (removedTag || addedTag)
                            {
                                isModified = true;
                            }

                        }

                    }

                    try
                    {
                        if (isModified)
                        {
                            // Save metadata changes
                            ItemInfo = "Saving MetaData: " + item.Location;
                            item.writeMetadata(MetadataFactory.WriteOptions.AUTO, this);

                            string info = "Completed updating Metadata for: " + item.Location;

                            InfoMessages.Add(info);
                            Logger.Log.Info(info);
                        }
                        else
                        {
                            string info = "Skipped updating Metadata (no changes) for: " + item.Location;

                            InfoMessages.Add(info);
                            Logger.Log.Info(info);
                        }

                        //rename and/or move
                        oldPath = FileUtils.getPathWithoutFileName(item.Location);
                        oldFilename = Path.GetFileNameWithoutExtension(item.Location);
                        String ext = Path.GetExtension(item.Location);

                        newFilename = parseNewFilename(state.Filename, oldFilename, counters, item.Metadata);
                        newPath = String.IsNullOrEmpty(state.Location) ? oldPath : state.Location;
                        newPath = newPath.TrimEnd('\\');

                        if (state.ImportedEnabled == true)
                        {
                            if (item.Metadata.IsImported == true && state.IsImported == false)
                            {
                                ItemInfo = "Exporting: " + item.Location;
                                MediaFileState.export(item, TokenSource.Token);

                                string info = "Exported: " + item.Location;

                                InfoMessages.Add(info);
                                Logger.Log.Info(info);
                            }
                        }

                        MediaFileState.move(item, newPath + "\\" + newFilename + ext, this);

                        if (state.ImportedEnabled == true)
                        {
                            if (item.Metadata.IsImported == false && state.IsImported == true)
                            {
                                ItemInfo = "Importing: " + item.Location;
                                MediaFileState.import(item, TokenSource.Token);

                                string info = "Imported: " + item.Metadata.Location;

                                InfoMessages.Add(info);
                                Logger.Log.Info(info);
                                
                            }

                        }

                        ItemProgress = 100;
                        TotalProgress++;
                       
                    }
                    catch (Exception e)
                    {
                        if (item.Metadata != null)
                        {
                            item.Metadata.clear();
                        }

                        // reload metaData in metadataviewmodel
                        item.readMetadata(MetadataFactory.ReadOptions.AUTO |
                            MetadataFactory.ReadOptions.GENERATE_THUMBNAIL, CancellationToken);

                        ItemInfo = "Error updating file: " + item.Location;
                        InfoMessages.Add(ItemInfo + " " + e.Message);
                        Logger.Log.Error(ItemInfo, e);
                        MessageBox.Show("Error updating file: " + item.Location + "\n\n" + e.Message,
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);                                               
                        return;
                    }

                }
                catch (Exception e)
                {
                    Logger.Log.Error("Error writing metadata", e);
                    MessageBox.Show("Error writing metadata", e.Message);
                  
                }
                finally
                {
                    item.RWLock.ExitUpgradeableReadLock();                

                    EventAggregator.GetEvent<MetaDataUpdateCompleteEvent>().Publish(item);                  
                }

            }
        
            if (!oldFilename.Equals(newFilename))
            {
                App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {

                    MiscUtils.insertIntoHistoryCollection(Settings.FilenameHistory, state.Filename);
                }));
            }

            if (!oldPath.Equals(newPath))
            {
                App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {

                    MiscUtils.insertIntoHistoryCollection(Settings.MetaDataUpdateDirectoryHistory, newPath);
                }));
            }


        }

        string parseNewFilename(string newFilename, string oldFilename, List<Counter> counters, BaseMetadata media)
        {
            if (String.IsNullOrEmpty(newFilename) || String.IsNullOrWhiteSpace(newFilename))
            {
                return (oldFilename);
            }

            List<Tuple<String, String>> replaceArgs = new List<Tuple<string, string>>();

            int nrCounters = 0;
            string outputFileName = "";

            for (int i = 0; i < newFilename.Length; i++)
            {

                if (newFilename[i].Equals('\"'))
                {
                    // grab substring
                    string subString = "";

                    int k = i + 1;

                    while (k < newFilename.Length && !newFilename[k].Equals('\"'))
                    {
                        subString += newFilename[k];
                        k++;
                    }

                    // replace
                    if (subString.Length > 0)
                    {
                        if (subString.StartsWith(oldFilenameMarker))
                        {
                            // insert old filename
                            outputFileName += oldFilename;
                        }
                        else if (subString.StartsWith(counterMarker))
                        {
                            // insert counter
                            nrCounters++;
                            int counterValue;
                            bool haveCounterValue = false;

                            if (counters.Count < nrCounters)
                            {
                                String stringValue = subString.Substring(counterMarker.Length);
                                haveCounterValue = int.TryParse(stringValue, out counterValue);

                                if (haveCounterValue)
                                {
                                    counters.Add(new Counter(counterValue, stringValue.Length));
                                }
                            }
                            else
                            {
                                counterValue = counters[nrCounters - 1].value;
                                haveCounterValue = true;
                            }

                            if (haveCounterValue)
                            {

                                outputFileName += counterValue.ToString("D" + counters[nrCounters - 1].nrDigits);

                                // increment counter
                                counters[nrCounters - 1].value += 1;
                            }
                        }
                        else if (subString.StartsWith(resolutionMarker))
                        {

                            int width = 0;
                            int height = 0;

                            if (media != null && media is ImageMetadata)
                            {
                                width = (media as ImageMetadata).Width;
                                height = (media as ImageMetadata).Height;
                            }
                            else if (media != null && media is VideoMetadata)
                            {
                                width = (media as VideoMetadata).Width;
                                height = (media as VideoMetadata).Height;
                            }

                            outputFileName += width.ToString() + "x" + height.ToString();

                        }
                        else if (subString.StartsWith(dateMarker))
                        {
                            String format = subString.Substring(dateMarker.Length);
                            String dateString = "";

                            if (media.CreationDate != null)
                            {
                                dateString = media.CreationDate.Value.ToString(format);
                            }

                            outputFileName += dateString;
                        }
                        else if (subString.StartsWith(replaceMarker))
                        {
                            String replaceString = subString.Substring(replaceMarker.Length);
                            int index = replaceString.IndexOf(';');

                            if (index == -1) continue;
                            Tuple<String, String> args = new Tuple<string,string>(replaceString.Substring(0,index),replaceString.Substring(index + 1));

                            if (String.IsNullOrEmpty(args.Item1) || String.IsNullOrWhiteSpace(args.Item1)) continue;
                            if (args.Item2 == null) continue;                                                       

                            replaceArgs.Add(args);
                        }
                    }

                    if (newFilename[k].Equals('\"'))
                    {
                        i = k;
                    }
                }
                else
                {
                    outputFileName += newFilename[i];
                }
            }

            foreach (Tuple<String, String> arg in replaceArgs)
            {
                outputFileName = outputFileName.Replace(arg.Item1, arg.Item2);
            }

            outputFileName = FileUtils.removeIllegalCharsFromFileName(outputFileName, "-");

            return (outputFileName);
        }

        public Command OkCommand { get; set; }
        public Command CancelCommand { get; set; }
        
        String itemInfo;

        public String ItemInfo
        {
            get { return itemInfo; }
            set
            {             
                SetProperty(ref itemInfo, value);
            }
        }

        ObservableCollection<String> infoMessages;

        public ObservableCollection<String> InfoMessages
        {
            get { return infoMessages; }
            set
            {             
                SetProperty(ref infoMessages, value);
            }
        }

        CancellationToken cancellationToken;

        public CancellationToken CancellationToken
        {
            get { return cancellationToken; }
            set { cancellationToken = value; }
        }

        int totalProgress;

        public int TotalProgress
        {
            get
            {
                return (totalProgress);
            }
            set
            {          
                SetProperty(ref totalProgress, value);
            }
        }

        int totalProgressMax;

        public int TotalProgressMax
        {
            get
            {
                return (totalProgressMax);
            }
            set
            {               
                SetProperty(ref totalProgressMax, value);
            }
        }

        int itemProgress;

        public int ItemProgress
        {
            get
            {
                return (itemProgress);
            }
            set
            {           
                SetProperty(ref itemProgress, value);
            }
        }

        int itemProgressMax;

        public int ItemProgressMax
        {
            get
            {
                return (itemProgressMax);
            }
            set
            {            
                SetProperty(ref itemProgressMax, value);
            }
        }
       
        String windowTitle;

        public String WindowTitle
        {
            get { return windowTitle; }
            set
            {            
                SetProperty(ref windowTitle, value);
            }
        }

        string windowIcon;

        public string WindowIcon
        {
            get
            {
                return (windowIcon);
            }
            set
            {             
                SetProperty(ref windowIcon, value);
            }
        }
    }

    public class MetaDataUpdateViewModelAsyncState
    {
        public MetaDataUpdateViewModelAsyncState(MetaDataViewModel vm)
        {
            Location = vm.Location;
            Author = vm.Author;
            AuthorEnabled = vm.AuthorEnabled;
            BatchMode = vm.BatchMode;
            Copyright = vm.Copyright;
            CopyrightEnabled = vm.CopyrightEnabled;
            Creation = vm.Creation;
            CreationEnabled = vm.CreationEnabled;
            Description = vm.Description;
            DescriptionEnabled = vm.DescriptionEnabled;
            Filename = vm.Filename;
            IsEnabled = vm.IsEnabled;

            lock (vm.Items)
            {
                ItemList = new List<MediaFileItem>(vm.Items);
            }
           
            Rating = vm.Rating;
            RatingEnabled = vm.RatingEnabled;
            Title = vm.Title;
            TitleEnabled = vm.TitleEnabled;
            IsImported = vm.IsImported;
            ImportedEnabled = vm.ImportedEnabled;
            Tags = new List<Tag>(vm.Tags);
            AddTags = new List<Tag>(vm.AddTags);
            RemoveTags = new List<Tag>(vm.RemoveTags);

            IsGeoTagEnabled = vm.IsGeotagEnabled;
            Latitude = vm.Geotag.LatDecimal;
            Longitude = vm.Geotag.LonDecimal;
        }

        public String Location { get; set; }
        public String Author { get; set; }
        public bool AuthorEnabled { get; set; }
        public bool BatchMode { get; set; }
        public String Copyright { get; set; }
        public bool CopyrightEnabled { get; set; }
        public String Description { get; set; }
        public bool DescriptionEnabled { get; set; }
        public String Filename { get; set; }
        public bool IsEnabled { get; set; }
        public List<MediaFileItem> ItemList { get; set; }
        public Nullable<double> Rating { get; set; }
        public bool RatingEnabled { get; set; }
        public String Title { get; set; }
        public bool TitleEnabled { get; set; }
        public List<Tag> Tags { get; set; }
        public List<Tag> AddTags { get; set; }
        public List<Tag> RemoveTags { get; set; }
        public Nullable<DateTime> Creation { get; set; }
        public bool CreationEnabled { get; set; }
        public bool IsImported { get; set; }
        public bool ImportedEnabled { get; set; }
        public Nullable<double> Latitude { get; set; }
        public Nullable<double> Longitude { get; set; }
        public bool IsGeoTagEnabled { get; set; }

    }
}
