﻿using MediaViewer.Logging;
using MediaViewer.MediaGrid;
using MediaViewer.Model.Media.File;
using MediaViewer.Model.Mvvm;
using MediaViewer.Model.Settings;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaViewer.DirectoryPicker
{
    class DirectoryPickerViewModel : CloseableBindableBase
    {            
        public DirectoryPickerViewModel()
        {
            MovePathHistory = new ObservableCollection<string>();

            OkCommand = new Command(new Action(() =>
            {
                OnClosingRequest(new DialogEventArgs(DialogMode.SUBMIT));
            }));

            CancelCommand = new Command(new Action(() =>
            {
                OnClosingRequest(new DialogEventArgs(DialogMode.CANCEL));

            }));

            InfoString = "";

            FavoriteLocations = AppSettings.Instance.FavoriteLocations;
        }

        Command okCommand;

        public Command OkCommand
        {
            get { return okCommand; }
            set { okCommand = value; }
        }
        Command cancelCommand;

        public Command CancelCommand
        {
            get { return cancelCommand; }
            set { cancelCommand = value; }
        }

        String infoString;

        public String InfoString
        {
            get
            {

                return (infoString);
            }

            set
            {                
                SetProperty(ref infoString, value);
            }

        }

        void updateInfoString()
        {      
            String temp = "Select Directory";

            if (SelectedItems != null)
            {

                temp += " - " + SelectedItems.Count.ToString() + " File(s) Selected: ";
                long sizeBytes = 0;

                foreach (MediaFileItem item in SelectedItems)
                {
                    FileInfo info = new FileInfo(item.Location);
                    sizeBytes += info.Length;

                }

                temp += MediaViewer.Model.Utils.MiscUtils.formatSizeBytes(sizeBytes);
            }

            InfoString = temp;
        }

        List<MediaFileItem> selectedItems;

        public List<MediaFileItem> SelectedItems
        {
            get { return selectedItems; }
            set
            {
                SetProperty(ref selectedItems, value);              
                             
                updateInfoString();
               
            }
        }

        String movePath;

        public String MovePath
        {
            get { return movePath; }
            set
            {              
                SetProperty(ref movePath, value);
            }
        }
        ObservableCollection<String> movePathHistory;

        public ObservableCollection<String> MovePathHistory
        {
            get { return movePathHistory; }
            set
            {               
                SetProperty(ref movePathHistory, value);
            }
        }

        ObservableCollection<String> favoriteLocations;

        public ObservableCollection<String> FavoriteLocations
        {
            get { return favoriteLocations; }
            set { SetProperty(ref favoriteLocations, value); }
        }
    }
}
