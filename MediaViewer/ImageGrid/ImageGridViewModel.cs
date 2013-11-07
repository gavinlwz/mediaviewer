﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaViewer.MediaFileModel;
using MvvmFoundation.Wpf;
using System.Windows.Data;
using System.Windows;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Specialized;
using MediaViewer.MediaFileModel.Watcher;

namespace MediaViewer.ImageGrid
{
    class ImageGridViewModel : ObservableObject
    {

        protected MediaFileState MediaFiles
        {
            get
            {
                return (MediaFileWatcher.Instance.MediaFiles);
            }
        }

        CancellationTokenSource loadItemsCTS;

        // maximum concurrently loading items
        const int maxLoadingItems = 25;
        // current concurrently loading items
        int nrLoadingItems;
        Object nrLoadingItemsLock;

        public ImageGridViewModel()
        {
           
            nrLoadingItems = 0;           

            nrLoadingItemsLock = new Object();
            loadItemsCTS = new CancellationTokenSource();
            
        }
             
             
        public void loadItemRangeAsync(int start, int nrItems)
        {
            // cancel any previously loading items           
            loadItemsCTS.Cancel();
            // create new cts for the items that need to be loaded
            loadItemsCTS = new CancellationTokenSource();

            MediaFiles.EnterReaderLock();

            for (int i = 0; i < nrItems; i++)
            {
                // don't reload already loaded items
                if (MediaFiles.Items[start + i].ItemState == MediaFileItemState.LOADED ||
                    MediaFiles.Items[start + i].ItemState == MediaFileItemState.LOADING) continue;

                lock (nrLoadingItemsLock)
                {
                    while (nrLoadingItems == maxLoadingItems)
                    {
                        Monitor.Wait(nrLoadingItemsLock);
                    }

                    nrLoadingItems = nrLoadingItems + 1;
                }

                MediaFiles.Items[start + i].loadMetaDataAsync(loadItemsCTS.Token).ContinueWith(finishedTask =>
                {
                    lock (nrLoadingItemsLock)
                    {
                        nrLoadingItems = nrLoadingItems - 1;

                        Monitor.PulseAll(nrLoadingItemsLock);
                    }
                });
                
                            
            }

            MediaFiles.ExitReaderLock();
            

        }
              
        
      
    }
}
