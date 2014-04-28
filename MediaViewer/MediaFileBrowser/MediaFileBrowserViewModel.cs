﻿using MediaViewer.DirectoryBrowser;
using MediaViewer.ImageGrid;
using MediaViewer.Input;
using MediaViewer.MediaFileModel;
using MediaViewer.MediaFileModel.Watcher;
using MediaViewer.DirectoryPicker;
using MediaViewer.Pager;
using MediaViewer.Search;
using MediaViewer.Utils;
using MvvmFoundation.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using MediaViewer.Import;
using MediaViewer.Export;
using MediaViewer.ImagePanel;
using VideoPlayerControl;

namespace MediaViewer.MediaFileBrowser
{
    class MediaFileBrowserViewModel : ObservableObject
    {

        static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
      
        private MediaFileWatcher mediaFileWatcher;
        private delegate void imageFileWatcherRenamedEventDelegate(System.IO.RenamedEventArgs e);       

        PagedImageGridViewModel pagedImageGridViewModel;

        public PagedImageGridViewModel PagedImageGridViewModel
        {
            get { return pagedImageGridViewModel; }
            set { pagedImageGridViewModel = value;                         
            }
        }

        ImageViewModel imageViewModel;

        internal ImageViewModel ImageViewModel
        {
            get { return imageViewModel; }
            set { imageViewModel = value; }
        }

        VideoPlayerViewModel videoViewModel;

        public VideoPlayerViewModel VideoViewModel
        {
            get { return videoViewModel; }
            set { videoViewModel = value; }
        }

        object currentViewModel;

        public object CurrentViewModel
        {
            get { return currentViewModel; }
            set { currentViewModel = value;
            NotifyPropertyChanged();
            }
        }

        public MediaFileBrowserViewModel() {

            ImageViewModel = new ImagePanel.ImageViewModel();
            ImageViewModel.SelectedScaleMode = ImagePanel.ImageViewModel.ScaleMode.AUTO;
            PagedImageGridViewModel = new ImageGrid.PagedImageGridViewModel(MediaFileWatcher.Instance.MediaState);

            //VideoViewModel = new VideoPlayerViewModel();

            CurrentViewModel = PagedImageGridViewModel;

            mediaFileWatcher = MediaFileWatcher.Instance;
         
            DeleteSelectedItemsCommand = new Command(new Action(deleteSelectedItems));

            ImportSelectedItemsCommand = new Command(() =>
            {
          
                ImportView import = new ImportView();
                import.ShowDialog();
                //ImportViewModel vm = (ImportViewModel)import.DataContext;
                //await vm.importAsync(selectedItems);

            });

            ExportSelectedItemsCommand = new Command(async () =>
            {
                List<MediaFileItem> selectedItems = MediaFileWatcher.Instance.MediaState.getSelectedItemsUIState();
                if (selectedItems.Count == 0) return;

                ExportView export = new ExportView();
                export.Show();
                ExportViewModel vm = (ExportViewModel)export.DataContext;
                await vm.exportAsync(selectedItems);

            });

            ExpandCommand = new Command(() =>
            {
                List<MediaFileItem> selectedItems = MediaFileWatcher.Instance.MediaState.getSelectedItemsUIState();
                if(selectedItems.Count == 0) return;

                String location = selectedItems[0].Location;

                if (Utils.MediaFormatConvert.isImageFile(location))
                {                 
                    GlobalMessenger.Instance.NotifyColleagues("MediaFileBrowser_ShowImage", location);
                }
                else if (Utils.MediaFormatConvert.isVideoFile(location))
                {
                    GlobalMessenger.Instance.NotifyColleagues("MediaFileBrowser_ShowVideo", location);
                }
            });

            ContractCommand = new Command(() =>
                {
                    GlobalMessenger.Instance.NotifyColleagues("MediaFileBrowser_ShowBrowserGrid");
                });

            GlobalMessenger.Instance.Register<String>("MediaFileBrowser_SetPath", (location) =>
            {
                BrowsePath = location;
            });


        }

        public string BrowsePath
        {

            set
            {
                FileInfo fileInfo = new FileInfo(value);

                string pathWithoutFileName;

                if ((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {

                    pathWithoutFileName = value;

                }
                else
                {

                    pathWithoutFileName = FileUtils.getPathWithoutFileName(value);

                }

                if (mediaFileWatcher.Path.Equals(pathWithoutFileName))
                {

                    return;
                }

                mediaFileWatcher.Path = pathWithoutFileName;
              
                GlobalMessenger.Instance.NotifyColleagues("MediaFileBrowser_PathSelected", value);

                NotifyPropertyChanged();
            }

            get
            {                
                return (mediaFileWatcher.Path);
            }
        }
     

        private void metaDataToolStripMenuItem_MouseDown(System.Object sender, ImageGridMouseEventArgs e)
        {
           
        }

        private void geoTagToolStripMenuItem_MouseDown(System.Object sender, ImageGridMouseEventArgs e)
        {
            
        }
    
        Command deleteSelectedItemsCommand;

        public Command DeleteSelectedItemsCommand
        {
            get { return deleteSelectedItemsCommand; }
            set { deleteSelectedItemsCommand = value; }
        }

        Command importSelectedItemsCommand;

        public Command ImportSelectedItemsCommand
        {
            get { return importSelectedItemsCommand; }
            set { importSelectedItemsCommand = value; }
        }

        Command exportSelectedItemsCommand;

        public Command ExportSelectedItemsCommand
        {
            get { return exportSelectedItemsCommand; }
            set { exportSelectedItemsCommand = value; }
        }

        Command expandCommand;

        public Command ExpandCommand
        {
            get { return expandCommand; }
            set { expandCommand = value; }
        }

        Command contractCommand;

        public Command ContractCommand
        {
            get { return contractCommand; }
            set { contractCommand = value; }
        }

        private void deleteSelectedItems()
        {

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            List<MediaFileItem> selected = MediaFileWatcher.Instance.MediaState.getSelectedItemsUIState();

            if (selected.Count == 0) return;
           
            if (MessageBox.Show("Are you sure you want to permanently delete " + selected.Count.ToString() + " file(s)?",
                "Delete Media", MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes)
            {
              
                try
                {
                    MediaFileWatcher.Instance.MediaState.delete(selected, tokenSource.Token);
                    
                }
                catch (Exception ex)
                {

                    log.Error("Error deleting file", ex);
                    MessageBox.Show(ex.Message, "Error deleting file");
                }
                
            }

        }

        private void renameImageToolStripMenuItem_MouseDown(System.Object sender, ImageGridMouseEventArgs e)
        {


        }
        private void selectAllToolStripMenuItem_MouseDown(System.Object sender, ImageGridMouseEventArgs e)
        {

           // imageGrid.setSelectedForAllImages(true);
        }
        private void deselectAllToolStripMenuItem_MouseDown(System.Object sender, ImageGridMouseEventArgs e)
        {

            //imageGrid.setSelectedForAllImages(false);
        }
        private void cutToolStripMenuItem_MouseDown(System.Object sender, ImageGridMouseEventArgs e)
        {

            
        }
        private void copyToolStripMenuItem_MouseDown(System.Object sender, ImageGridMouseEventArgs e)
        {

           
        }


        private void moveButton_Click(System.Object sender, System.EventArgs e)
        {
            /*
                             List<MediaPreviewAsyncState > images = imageGrid.getSelectedImageData();	
                             if(images.Count == 0) return;

                             FolderBrowserDialog dialog = new FolderBrowserDialog();

                             dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                             dialog.SelectedPath = mediaFileWatcher.Path;
                             dialog.Description = L"Move " + Convert.ToString(images.Count) + L" image(s) to";

                             if(dialog.ShowDialog() == DialogResult.OK) {

                                 for(int i = 0; i < images.Count; i++) {

                                     string fileName = Path.GetFileName(images[i].ImageLocation);
                                     string destFileName = dialog.SelectedPath + "\\" + fileName;

                                     if(File.Exists(destFileName)) {

                                         DialogResult result = MessageBox.Show(destFileName + L"\n already exists, do you want to overwrite?", "Overwrite File?",
                                             MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                                         if(result == DialogResult.Yes) {

                                             File.Delete(destFileName);

                                         } else if(result == DialogResult.No) {

                                             continue;

                                         } else if(result == DialogResult.Cancel) {

                                             break;
                                         }

                                     }

                                     File.Move(images[i].ImageLocation, destFileName);

                                 }

                             }
             */
        }
    

        private void imageGrid_MouseEnter(System.Object sender, System.EventArgs e)
        {


        }
        private void directoryBrowser_MouseEnter(System.Object sender, System.EventArgs e)
        {


        }
        private void directoryBrowser_Renamed(System.Object sender, System.IO.RenamedEventArgs e)
        {

            // if(mediaFileWatcher.Path.Equals(e.OldFullPath)) {

            BrowsePath = e.FullPath;
            //}
        }
    }
}
