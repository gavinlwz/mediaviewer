﻿using MediaViewer.ImageGrid;
using MediaViewer.MediaFileModel.Watcher;
using MediaViewer.Utils;
using MvvmFoundation.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaViewer.MoveRename
{
    class MoveRenameViewModel : ObservableObject
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        const string oldFilenameMarker = "@";
        const string counterMarker = "&";

        public MoveRenameViewModel()
        {
            MovePathHistory = new ObservableCollection<string>();

            RenameFileName = "\"" + oldFilenameMarker + "\"";
            RenameExtension = "";

            insertOldFilenameCommand = new Command<int>(new Action<int>((startIndex) =>
            {
                try
                {
                    RenameFileName = RenameFileName.Insert(startIndex, "\"" + oldFilenameMarker + "\"");
                }
                catch (Exception e)
                {
                    log.Error(e);
                }

            }));

            insertCounterCommand = new Command<Tuple<int, int>>(new Action<Tuple<int, int>>((args) =>
            {
                try
                {
                    int startIndex = args.Item1;
                    int counter = args.Item2;

                    RenameFileName = RenameFileName.Insert(startIndex, "\"" + counterMarker + counter.ToString() + "\"");
                }
                catch (Exception e)
                {
                    log.Error(e);
                }

            }));

            moveRenameFilesCommand = new Command(new Action(() =>
            {

                moveRenameFiles();
            }));

            ModeList = new ObservableCollection<string>();
            ModeList.Add("Copy");
            ModeList.Add("Move");
            ModeList.Add("Rename Only");

            IsMove = true;
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
                infoString = value;
                NotifyPropertyChanged();
            }

        }

        void updateInfoString()
        {
            String temp = IsCopy ? "Copy/Rename" : "Move/Rename";

            if (SelectedItems != null)
            {

                temp += " " + SelectedItems.Count.ToString() + " File(s) - ";
                long sizeBytes = 0;

                foreach (MediaFileItem item in SelectedItems)
                {
                    FileInfo info = new FileInfo(item.Location);
                    sizeBytes += info.Length;

                }

                temp += Utils.Misc.formatSizeBytes(sizeBytes);
            }

            InfoString = temp;
        }

        List<MediaFileItem> selectedItems;

        public List<MediaFileItem> SelectedItems
        {
            get { return selectedItems; }
            set
            {
                selectedItems = value;
                             
                updateInfoString();
               
                NotifyPropertyChanged();
            }
        }

        String movePath;

        public String MovePath
        {
            get { return movePath; }
            set
            {
                movePath = value;
                NotifyPropertyChanged();
            }
        }
        ObservableCollection<String> movePathHistory;

        public ObservableCollection<String> MovePathHistory
        {
            get { return movePathHistory; }
            set
            {
                movePathHistory = value;
                NotifyPropertyChanged();
            }
        }

        Command<int> insertOldFilenameCommand;

        public Command<int> InsertOldFilenameCommand
        {
            get { return insertOldFilenameCommand; }
            set { insertOldFilenameCommand = value; }
        }
        Command<Tuple<int, int>> insertCounterCommand;

        public Command<Tuple<int, int>> InsertCounterCommand
        {
            get { return insertCounterCommand; }
            set { insertCounterCommand = value; }
        }

        String renameFileName;

        public String RenameFileName
        {
            get { return renameFileName; }
            set
            {
                renameFileName = value;
                NotifyPropertyChanged();
            }
        }
        ObservableCollection<String> renameFileNameHistory;

        public ObservableCollection<String> RenameFileNameHistory
        {
            get { return renameFileNameHistory; }
            set { renameFileNameHistory = value; }
        }

        String renameExtension;

        public String RenameExtension
        {
            get { return renameExtension; }
            set
            {
                renameExtension = value;
                NotifyPropertyChanged();
            }
        }
        ObservableCollection<String> renameExtensionHistory;

        public ObservableCollection<String> RenameExtensionHistory
        {
            get { return renameExtensionHistory; }
            set { renameExtensionHistory = value; }
        }

        Command moveRenameFilesCommand;

        public Command MoveRenameFilesCommand
        {
            get { return moveRenameFilesCommand; }
            set { moveRenameFilesCommand = value; }
        }

        ObservableCollection<String> modeList;

        public ObservableCollection<String> ModeList
        {
            get { return modeList; }
            set
            {
                modeList = value;
                NotifyPropertyChanged();
            }
        }

        bool isMove;

        public bool IsMove
        {
            get { return isMove; }
            set
            {
                isMove = value;
                if (isMove == true && IsCopy == true)
                {
                    IsCopy = false;
                }
                else if (isMove == false && IsCopy == false)
                {
                    IsCopy = true;
                }
                updateInfoString();
                NotifyPropertyChanged();
            }
        }
        bool isCopy;

        public bool IsCopy
        {
            get { return isCopy; }
            set
            {
                isCopy = value;
                if (isCopy == true && isMove == true)
                {
                    IsMove = false;
                }
                else if(isCopy == false && isMove == false)
                {
                    IsMove = true;
                }
                updateInfoString();
                NotifyPropertyChanged();
            }
        }

        async void moveRenameFiles()
        {

            bool success = MediaFileWatcher.Instance.MediaFilesInUseByOperation.AddRange(SelectedItems);

            try
            {
                FileUtils fileUtils = new FileUtils();

                List<int> counters = new List<int>();
                StringCollection sourcePaths = new StringCollection();
                StringCollection destPaths = new StringCollection();
              
                foreach (MediaFileItem item in SelectedItems)
                {
                    sourcePaths.Add(item.Location);
                    String sourceFilenameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(item.Location);
                    String sourceExtension = System.IO.Path.GetExtension(item.Location);

                    String destFileNameWithoutExtension = parseNewFilename(RenameFileName, sourceFilenameWithoutExtension, counters);
                    String destExtension = parseNewExtension(RenameExtension, sourceExtension);

                    destPaths.Add(MovePath + "\\" + destFileNameWithoutExtension + destExtension);
                }

                MoveRenameProgressWindow progressWindow = new MoveRenameProgressWindow();
                progressWindow.Show();

                FileUtilsProgressViewModel progress = (FileUtilsProgressViewModel)progressWindow.DataContext;

                if (success == false)
                {
                    progress.ItemInfo = "Cannot copy/move selected file(s), some are already scheduled for another operation";
                    return;

                } else if (IsCopy)
                {

                    Action method = () => fileUtils.copy(sourcePaths, destPaths, progress);

                    await Task.Run(method, progress.CancellationToken);

                }
                else
                {
                    Action method = () => fileUtils.move(sourcePaths, destPaths, progress);

                    await Task.Run(method, progress.CancellationToken);
                }
            }
            finally
            {
                MediaFileWatcher.Instance.MediaFilesInUseByOperation.RemoveAll(SelectedItems);
            }


        }

        string parseNewFilename(string newFilename, string oldFilename, List<int> counters)
        {
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
                        if (subString[0].Equals(oldFilenameMarker[0]))
                        {
                            // insert old filename
                            outputFileName += oldFilename;
                        }
                        else if (subString[0].Equals(counterMarker[0]))
                        {
                            // insert counter
                            nrCounters++;
                            int counterValue;
                            bool haveCounterValue = false;

                            if (counters.Count < nrCounters)
                            {
                                haveCounterValue = int.TryParse(subString.Substring(1), out counterValue);

                                if (haveCounterValue)
                                {
                                    counters.Add(counterValue);
                                }
                            }
                            else
                            {
                                counterValue = counters[nrCounters - 1];
                                haveCounterValue = true;
                            }

                            if (haveCounterValue)
                            {

                                outputFileName += counterValue.ToString();

                                // increment counter
                                counters[nrCounters - 1] += 1;
                            }
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

            return (outputFileName);
        }

        string parseNewExtension(string newExtension, string oldExtension)
        {
            if (String.IsNullOrEmpty(newExtension))
            {
                return (oldExtension);
            }
            else
            {
                return (newExtension);
            }
        }
    }
}
