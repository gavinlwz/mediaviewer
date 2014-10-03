﻿using MediaViewer.Model.Media.File;
using MediaViewer.Model.GlobalEvents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MediaViewer.Model.Utils;
using MediaViewer.Model.Media.State.CollectionView;

namespace MediaViewer.MediaGrid
{
    /// <summary>
    /// Interaction logic for MediaGridItemBasicView.xaml
    /// </summary>
    public partial class MediaGridItemBasicView : UserControl
    {


        public MediaGridItemBasicView()
        {
            InitializeComponent();
        }

        public SelectableMediaFileItem SelectableMediaFileItem
        {
            get { return (SelectableMediaFileItem)GetValue(MediaFileItemProperty); }
            set { SetValue(MediaFileItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectableMediaFileItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MediaFileItemProperty =
            DependencyProperty.Register("SelectableMediaFileItem", typeof(SelectableMediaFileItem), typeof(MediaGridItemBasicView), new PropertyMetadata(null));

        private void viewMenuItem_Click(object sender, RoutedEventArgs e)
        {
           
                  

        }
         
        public bool IsGridLoaded
        {
            get { return (bool)GetValue(IsGridLoadedProperty); }
            set { SetValue(IsGridLoadedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsGridLoaded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsGridLoadedProperty =
            DependencyProperty.Register("IsGridLoaded", typeof(bool), typeof(MediaGridItemBasicView), new PropertyMetadata(true, isGridLoadedChangedCallback));

        private static void isGridLoadedChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MediaGridItemBasicView item = d as MediaGridItemBasicView;

            item.selectAllMenuItem.IsEnabled = !(bool)e.NewValue;

        }

        private void selectAllMenuItem_Click(object sender, RoutedEventArgs e)
        {

            MediaStateCollectionView cv = (this.Tag as MediaStateCollectionView);
            cv.selectAll();
        }

        private void deselectAllMenuItem_Click(object sender, RoutedEventArgs e)
        {

            MediaStateCollectionView cv = (this.Tag as MediaStateCollectionView);
            cv.deselectAll();

        }

        private void browseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MediaFileItem item = SelectableMediaFileItem.Item;

            String location = FileUtils.getPathWithoutFileName(item.Location);

            GlobalMessenger.Instance.NotifyColleagues("MediaFileBrowser_SetPath", location);
        }

        private void openInExplorerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MediaFileItem item = SelectableMediaFileItem.Item;

            String location = FileUtils.getPathWithoutFileName(item.Location);

            Process.Start(location);
        }



        private void imageGridItem_Checked(object sender, RoutedEventArgs e)
        {
            SelectableMediaFileItem item = (SelectableMediaFileItem)DataContext;

            if (item.IsSelected == true) return;
          
            MediaStateCollectionView cv = (this.Tag as MediaStateCollectionView);
            cv.deselectAll();
           
            item.IsSelected = true;
         
        }

        private void imageGridItem_Unchecked(object sender, RoutedEventArgs e)
        {
            SelectableMediaFileItem item = (SelectableMediaFileItem)DataContext;

            item.IsSelected = false;
         
          
        }
          
    }
}
