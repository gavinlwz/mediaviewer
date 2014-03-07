﻿using MediaViewer.MediaDatabase;
using MediaViewer.MediaDatabase.DbCommands;
using MediaViewer.Progress;
using MediaViewer.Utils;
using MvvmFoundation.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MediaViewer.MediaFileModel.Watcher
{
    public class MediaFileItem : ObservableObject, IEquatable<MediaFileItem>, IComparable<MediaFileItem>
    {

        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        MediaFileItemState state;

        public MediaFileItemState ItemState
        {
            get { return state; }
            set
            {
                state = value;
                NotifyPropertyChanged();
            }
        }
  
        public MediaFileItem(String location)
        {
            Location = location;
            IsSelected = false;
            Media = null;
            ItemState = MediaFileItemState.EMPTY;          
        }
       
        string location;

        public string Location
        {
            get { return location; }
            set
            {
                location = value;
                NotifyPropertyChanged();
            }
        }
      
        bool isSelected;

        public bool IsSelected
        {
            get
            {
                return (isSelected);
            }
            set
            {
                isSelected = value;
                NotifyPropertyChanged();

            }
        }

        public void toggleSelected()
        {
            if (IsSelected == true)
            {
                IsSelected = false;
            }
            else
            {
                IsSelected = true;
            }
        }

        Media media;

        public Media Media
        {
            get { return media; }
            set
            {
                media = value;
              

                NotifyPropertyChanged();
            }
        }
    
        public void writeMetaData(MediaFactory.WriteOptions options, IProgress progress)
        {
            if (media != null)
            {
                MediaFactory.write(Media, options, progress);
            }
        }

        MediaFileItemState getMediaState(Media media)
        {
            MediaFileItemState result = MediaFileItemState.LOADED;

            if (media == null || media is UnknownMedia)
            {
                result = MediaFileItemState.ERROR;
            }
            else if (media.MetadataReadError != null)
            {
                if (media.MetadataReadError is FileNotFoundException)
                {
                    result = MediaFileItemState.FILE_NOT_FOUND;
                }           
                else
                {
                    result = MediaFileItemState.ERROR;
                }
            }

            return (result);
        }


        public void readMetaData(MediaFactory.ReadOptions options, CancellationToken token)
        {
            ItemState = MediaFileItemState.LOADING;

            Media media = null;
            MediaFileItemState result = MediaFileItemState.LOADED;

            try
            {
                media = MediaFactory.read(Location, options, token);

                result = getMediaState(media); 
               
            }
            catch (TaskCanceledException)
            {
                result = MediaFileItemState.CANCELLED;
            }         
            catch (Exception e)
            {
                result = MediaFileItemState.ERROR;
                log.Info("Error loading image grid item:" + Location, e);

            }

            ItemState = result;
            Media = media;
        }

        public async Task readMetaDataAsync(MediaFactory.ReadOptions options, CancellationToken token)
        {

            ItemState = MediaFileItemState.LOADING;

            Media media = null;
            MediaFileItemState result = MediaFileItemState.LOADED;

            try
            {
                media = await MediaFactory.readAsync(Location, options, token).ConfigureAwait(false);

                result = getMediaState(media);
            }
            catch (TaskCanceledException)
            {
                result = MediaFileItemState.CANCELLED;
            }          
            catch (Exception e)
            {
                result = MediaFileItemState.ERROR;
                log.Info("Error loading image grid item:" + Location, e);

            }

            // assign the results on the UI thread           
            DispatcherOperation task = Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Media = media;
                ItemState = result;               
            }));

        }

        /// <summary>
        /// Returns true if deleted file was imported otherwise false
        /// </summary>
        /// <returns></returns>
        public bool delete()
        {
            bool isImported = false;

            if (ItemState == MediaFileItemState.DELETED)
            {
                return (isImported);
            }

            FileUtils fileUtils = new FileUtils();
           
            fileUtils.deleteFile(Location);
         
            if (Media != null && Media.IsImported)
            {
                using (MediaDbCommands mediaCommands = new MediaDbCommands())
                {
                    mediaCommands.delete(Media);
                }

                Media = null;         
            }

            ItemState = MediaFileItemState.DELETED;
            return (isImported);
        }


      

        /// <summary>
        /// returns true if the moved item was a imported item otherwise false
        /// </summary>
        /// <param name="newLocation"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public bool move(String newLocation, IProgress progress)
        {
            bool isImported = false;

            if (ItemState == MediaFileItemState.DELETED)
            {
                return (isImported);
            }
          
            // A subtle bug can occur when renaming files if updating Media in the database is done AFTER the file is renamed,
            // Consider the following sequence of events:
            // The file is renamed. A new file is "created" with it's new name. This file gets loaded, 
            // but it's Media entity in the database is not yet updated to the new location.
            // So it will be loaded as a non-imported file.
            // To avoid this error, update Media in the database before it is moved. In case a move error occurs
            // we change media back to it's old location
            //updateMediaLocation(media, newLocation);

            FileUtils fileUtils = new FileUtils();
           
            fileUtils.moveFile(Location, newLocation, progress);

            if (Media != null)
            {
                Media.Location = newLocation;

                if (Media.IsImported)
                {
                    using (MediaDbCommands mediaCommands = new MediaDbCommands())
                    {
                        Media = mediaCommands.update(Media);
                    }
                }
            }


            // A delete event will be fired by the mediafilewatcher for the current item with it's old location.
            // If location is changed to it's new location it will not be be found in the current mediastate. 
            // So only update the location when mediafilewatcher is not active.
            if (MediaFileWatcher.Instance.IsWatcherEnabled == false)
            {
                Location = newLocation;
            }
            
            return (isImported);
        }

        public bool import()
        {
            if (ItemState == MediaFileItemState.DELETED || media == null || media.IsImported == true)
            {
                return (false);
            }

            using (MediaDbCommands mediaCommands = new MediaDbCommands())
            {
                Media = mediaCommands.create(Media);
            }    

            return (true);
        }

        public bool export()
        {
            if (ItemState == MediaFileItemState.DELETED || media == null || media.IsImported == false)
            {
                return (false);
            }

            using (MediaDbCommands mediaCommands = new MediaDbCommands())
            {
                mediaCommands.delete(Media);
            }  

            return (true);
        }

   
        public bool Equals(MediaFileItem other)
        {
            if (other == null) return (false);

            return (Location.Equals(other.Location));
        }

        public int CompareTo(MediaFileItem other)
        {
            return(Path.GetFileName(Location).CompareTo(Path.GetFileName(other.Location)));
        }
    }
}
