﻿using MediaViewer.Infrastructure.Logging;
using MediaViewer.MediaDatabase;
using MediaViewer.MediaDatabase.DbCommands;
using MediaViewer.Model.Media.Base;
using MediaViewer.Model.metadata.Metadata;
using MediaViewer.Model.Utils;
using MediaViewer.Progress;
using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MediaViewer.Model.Media.File
{
    public class MediaFileItem : MediaItem
    {
        // If IsBufferEvents is true events are collected in a buffer so they can be fired outside locks
        // The events will be executed when IsBufferEvents is set to false
        ConcurrentQueue<String> eventBuffer;

        protected MediaFileItem(String location, MediaItemState state = MediaItemState.EMPTY)
            : base(location, Path.GetFileNameWithoutExtension(location), state)
        {
            eventBuffer = new ConcurrentQueue<string>();
            isBufferEvents = false;
        }
                      
        /// <summary>
        /// Location on disk of the mediafileitem
        /// </summary>
        public override string Location
        {
            get
            {
                return (location);
            }
            set
            {
                bool isLocationChanged;
                bool isMetadataChanged;

                setLocation(value, out isLocationChanged, out isMetadataChanged);

                if (isLocationChanged)
                {
                    OnPropertyChangedBuffered("Location");
                    OnPropertyChangedBuffered("Name");
                }

                if (isMetadataChanged)
                {
                    OnPropertyChangedBuffered("Metadata");
                }
            }
        }

        void setLocation(String newLocation, out bool isLocationChanged, out bool isMetadataChanged)
        {
            isMetadataChanged = false;
            isLocationChanged = false;

            RWLock.EnterWriteLock();
            try
            {
                String oldLocation = location;               

                if (!String.IsNullOrEmpty(oldLocation) && !String.IsNullOrEmpty(newLocation))
                {
                    // check if newLocation has changed
                    if (oldLocation.Equals(newLocation)) return;

                    isLocationChanged = true;

                    // update newLocation in dictionary
                    Factory.renameInDictionary(oldLocation, newLocation);

                    // update newLocation in the database
                    // Note: don't use the base class getters/setters for metadata to
                    // prevent triggering a onpropertychanged event while holding a write lock
                    if (metadata != null)
                    {
                        metadata.Location = newLocation;

                        if (metadata.IsImported)
                        {
                            using (MetadataDbCommands metadataCommands = new MetadataDbCommands())
                            {
                                metadata = metadataCommands.update(metadata);
                                isMetadataChanged = true;
                            }
                        }
                    }
                    else
                    {
                        using (MetadataDbCommands metadataCommands = new MetadataDbCommands())
                        {
                            metadata = metadataCommands.findMetadataByLocation(oldLocation);
                            if (metadata != null)
                            {
                                metadata.Location = newLocation;
                                metadata = metadataCommands.update(Metadata);
                            }

                            isMetadataChanged = true;
                        }

                    }
                }
                else if (String.IsNullOrEmpty(newLocation) && !String.IsNullOrEmpty(oldLocation))
                {
                    Factory.deleteFromDictionary(oldLocation);
                }

                location = newLocation;
                name = Path.GetFileNameWithoutExtension(newLocation);
            }
            finally
            {
                RWLock.ExitWriteLock();                
            }
        }

                         
        public void writeMetadata(MetadataFactory.WriteOptions options, CancellableOperationProgressBase progress)
        {
            // this can be a read lock since the mediafileitem instance is not modified during writing to disk.
            // but we don't want multiple writes at the same time so we use the upgradablereadlock
            bool metadataChanged = false;
            bool varsChanged = false;

            RWLock.EnterUpgradeableReadLock();
            try
            {
                if (Metadata != null)
                {
                    MetadataFactory.write(Metadata, options, progress);
                    metadataChanged = true;

                    RWLock.EnterWriteLock();
                    varsChanged = checkVariables(Metadata);
                    RWLock.ExitWriteLock();
                }
            }
            finally
            {
                RWLock.ExitUpgradeableReadLock();                                         
            }

            if (metadataChanged)
            {
                OnPropertyChangedBuffered("Metadata");
            }

            if (varsChanged)
            {
                OnPropertyChangedBuffered("HasTags");
                OnPropertyChangedBuffered("HasGeoTag");
            }

        }

        public override void readMetadata(MetadataFactory.ReadOptions options, CancellationToken token)
        {
            BaseMetadata metadataTemp = null;
            MediaItemState itemStateTemp = MediaItemState.LOADED;

            ItemState = MediaItemState.LOADING;

            try {
                
                metadataTemp = MetadataFactory.read(Location, options, token);

                if (metadataTemp == null || metadataTemp is UnknownMetadata)
                {
                    itemStateTemp = MediaItemState.ERROR;
                }
                else {

                    if (metadataTemp.MetadataReadError != null)
                    {
                        if (metadataTemp.MetadataReadError is FileNotFoundException)
                        {
                            itemStateTemp = MediaItemState.FILE_NOT_FOUND;
                        }
                        else
                        {
                            itemStateTemp = MediaItemState.ERROR;
                        }
                    }                   
                }

            }
            catch(TimeoutException) {

                itemStateTemp = MediaItemState.TIMED_OUT;              
            }
            catch (Exception e)
            {
                itemStateTemp = MediaItemState.ERROR;
                Logger.Log.Info("Error loading image grid item:" + Location, e);
            }
            finally
            {
                RWLock.EnterWriteLock();

                metadata = metadataTemp;
                itemState = itemStateTemp;

                bool changed = checkVariables(metadata);  

                RWLock.ExitWriteLock();

                OnPropertyChangedBuffered("ItemState");
                OnPropertyChangedBuffered("Metadata");                

                if (changed)
                {
                    OnPropertyChangedBuffered("HasTags");
                    OnPropertyChangedBuffered("HasGeoTag");
                }
                                           
            }
        }

        // function should only be called inside a write lock
        bool checkVariables(BaseMetadata metadata)
        {
            if (metadata == null) return(false);

            if (metadata.Tags.Count > 0)
            {
                hasTags = true;
            }
            else
            {
                hasTags = false;
            }

            if (metadata.Longitude != null && metadata.Latitude != null)
            {
                hasGeoTag = true;
            }
            else
            {
                hasGeoTag = false;
            }

            return (true);

        }
        

        /// <summary>
        /// Returns true if deleted file was imported otherwise false
        /// </summary>
        /// <returns></returns>
        public bool delete()
        {
            RWLock.EnterWriteLock();
            try
            {
                bool isImported = false;

                if (ItemState == MediaItemState.DELETED)
                {
                    return (isImported);
                }

                FileUtils fileUtils = new FileUtils();

                if (ItemState != MediaItemState.FILE_NOT_FOUND)
                {
                    fileUtils.deleteFile(Location);
                }

                if (Metadata != null && Metadata.IsImported)
                {
                    using (MetadataDbCommands metadataCommands = new MetadataDbCommands())
                    {
                        metadataCommands.delete(Metadata);
                    }

                    metadata = null;

                    isImported = true;
                }

                itemState = MediaItemState.DELETED;
                //Factory.deleteFromDictionary(location);

                return (isImported);
            }
            finally
            {
                RWLock.ExitWriteLock();

                OnPropertyChangedBuffered("Metadata");
                OnPropertyChangedBuffered("ItemState");
            }
           
        }
      
        /// <summary>
        /// returns true if the moved item was a imported item otherwise false
        /// </summary>
        /// <param name="newLocation"></param>
        /// <param name="progress"></param>        
        /// <returns></returns>
        public bool move(String newLocation, CancellableOperationProgressBase progress)
        {
            bool isLocationChanged = false;
            bool isMetadataChanged = false; 

            RWLock.EnterUpgradeableReadLock();
            try
            {                
                bool isImported = false;

                if (ItemState == MediaItemState.DELETED)
                {
                    return (isImported);
                }
               
                FileUtils fileUtils = new FileUtils();

                fileUtils.moveFile(Location, newLocation, progress);
                           
                // A delete event will be fired by the mediafilewatcher for the current item with it's old location.
                // If location is changed to it's new location it will not be be found in the current mediastate. 
                // So only update the location when mediafilewatcher is not active.               
                setLocation(newLocation, out isLocationChanged, out isMetadataChanged);
                                             
                return (isImported = Metadata.IsImported);
            }
            finally
            {
                RWLock.ExitUpgradeableReadLock();

                if (isLocationChanged)
                {
                    OnPropertyChangedBuffered("Location");
                    OnPropertyChangedBuffered("Name");
                }

                if (isMetadataChanged)
                {
                    OnPropertyChangedBuffered("Metadata");
                }
            }
            
        }
            

        public bool import(CancellationToken token)
        {
            RWLock.EnterUpgradeableReadLock();
            try
            {
                if (ItemState == MediaItemState.DELETED || Metadata == null || Metadata.IsImported == true)
                {
                    return (false);
                }

                using (MetadataDbCommands metadataCommands = new MetadataDbCommands())
                {
                    RWLock.EnterWriteLock();
                    metadata = metadataCommands.create(Metadata);
                    RWLock.ExitWriteLock();
                }                
            }
            finally
            {
                RWLock.ExitUpgradeableReadLock();
            }

            OnPropertyChangedBuffered("Metadata");

            return (true);
        }

        public bool export(CancellationToken token)
        {
            RWLock.EnterReadLock();
            try
            {
                if (ItemState == MediaItemState.DELETED || Metadata == null || Metadata.IsImported == false)
                {
                    return (false);
                }

                using (MetadataDbCommands metadataCommands = new MetadataDbCommands())
                {
                    metadataCommands.delete(Metadata);
                   
                }               
            }
            finally
            {
                RWLock.ExitReadLock();
            }

            OnPropertyChangedBuffered("Metadata");
            return (true);
        }
       
 
        public override int CompareTo(MediaItem other)
        {
            if (other == null)
            {
                throw new ArgumentException();
            }          
            RWLock.EnterReadLock();
            try
            {
                other.RWLock.EnterReadLock();
                try
                {
                    return (Path.GetFileName(Location).CompareTo(Path.GetFileName(other.Location)));
                }
                finally
                {
                    other.RWLock.ExitReadLock();
                }
            }
            finally
            {
                RWLock.ExitReadLock();
            }

        }

        bool isBufferEvents;

        /// <summary>
        /// If true buffers propertychanged events
        /// If false flush the buffered propertychanged events
        /// </summary>
        public bool IsBufferEvents
        {
            get { return isBufferEvents; }
            set {
                
                isBufferEvents = value;
                
                // Note: A event could be fired here before any queued events are flushed.
                // resulting in events occuring out of order.             
                if (value == false)
                {
                    // flush queued events
                    String propertyName;

                    while (eventBuffer.TryDequeue(out propertyName))
                    {
                        OnPropertyChanged(propertyName);
                    }                    
                }

                
            }
        }

        void OnPropertyChangedBuffered(String propertyName)
        {
            if (IsBufferEvents)
            {
                eventBuffer.Enqueue(propertyName);
            }
            else
            {
                OnPropertyChanged(propertyName);
            }
        }


        // factory to ensure mediafileitems are globally unique
        public class Factory
        {
            public static MediaFileItem EmptyItem = new MediaFileItem("");
          
            static ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();
            static Dictionary<String, WeakReference<MediaFileItem>> dictionary = new Dictionary<String, WeakReference<MediaFileItem>>();

            public static MediaFileItem create(string location)
            {                               
                rwLock.EnterWriteLock();
                try
                {  
                    WeakReference<MediaFileItem> reference = null;
                    MediaFileItem item = null;

                    bool success = dictionary.TryGetValue(location, out reference);

                    if (success == true)
                    {
                        bool exists = reference.TryGetTarget(out item);

                        if (exists == false) {
                      
                            // item has been garbage collected, recreate
                            item = new MediaFileItem(location);
                            reference = new WeakReference<MediaFileItem>(item);
                            dictionary.Remove(location);
                            dictionary.Add(location, reference);
                            
                        }
                    }
                    else
                    {
                        // item did not exist yet
                        item = new MediaFileItem(location);
                        reference = new WeakReference<MediaFileItem>(item);
                        dictionary.Add(location, reference);
                  
                    }

                    return (item);
                }
                finally
                {
                    rwLock.ExitWriteLock();                   
                }
            }

            public static void renameInDictionary(String oldLocation, String newLocation)
            {
                rwLock.EnterWriteLock();
                try
                {
                    WeakReference<MediaFileItem> reference = null;
                    MediaFileItem item = null;

                    bool potentialFailure = dictionary.TryGetValue(newLocation, out reference);

                    if (potentialFailure == true)
                    {
                        if (reference.TryGetTarget(out item))
                        {
                            if (item.ItemState == MediaItemState.DELETED)
                            {
                                // the mediafileitem in the hash has been deleted on disk
                                dictionary.Remove(newLocation);
                            }
                            else
                            {
                                // there is a live mediafileitem in the hash clashing with the newly renamed item
                                throw new InvalidOperationException("Trying to rename item to existing item in media dictionary: " + oldLocation + " to " + newLocation);
                            }
                        }
                        else
                        {
                            // the mediafileitem in the hash is already dead
                            dictionary.Remove(newLocation);
                        }
                    }

                    bool success = dictionary.TryGetValue(oldLocation, out reference);
                    
                    if (success == true)
                    {
                        bool exists = reference.TryGetTarget(out item);

                        if (exists == false)
                        {
                            // item has been garbage collected, recreate
                            dictionary.Remove(oldLocation);
                            item = new MediaFileItem(newLocation);
                            reference = new WeakReference<MediaFileItem>(item);

                            dictionary.Add(newLocation, reference);                          
                        }
                        else
                        {
                            dictionary.Remove(oldLocation);
                            dictionary.Add(newLocation, reference);
                        }
                    }
                    else
                    {
                        // item does not exist
                        throw new InvalidOperationException("Trying to rename non-existing item in media dictionary: " + oldLocation + " to " + newLocation);
                       
                    }
                 
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }

            public static void deleteFromDictionary(string location)
            {
                rwLock.EnterWriteLock();
                try
                {                 
                    dictionary.Remove(location);                    
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }

            public static MediaFileItem findInDictionary(string location)
            {
                rwLock.EnterWriteLock();
                try
                {
                    WeakReference<MediaFileItem> reference = null;
                    MediaFileItem item = null;

                    bool success = dictionary.TryGetValue(location, out reference);

                    if (success == true)
                    {
                        bool exists = reference.TryGetTarget(out item);

                        if (exists == false)
                        {
                            return (null);
                        }
                    }
                   
                    return (item);
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }
        }
              
    }
}
