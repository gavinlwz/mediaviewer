﻿using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
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
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.PubSubEvents;
using MediaViewer.Model.GlobalEvents;
using MediaViewer.Model.Media.State.CollectionView;
using MediaViewer.Model.Media.File;
using MediaViewer.Pager;
using MvvmFoundation.Wpf;
using System.ComponentModel;

namespace MediaViewer.MediaGrid
{
    /// <summary>
    /// Interaction logic for MediaStackPanelView.xaml
    /// </summary>
    [Export]
    public partial class MediaStackPanelView : UserControl, INavigationAware   
    {
        ScrollViewer scrollViewer;
        int scrollToIndex;

        MediaStackPanelViewModel ViewModel { get; set; }
        IEventAggregator EventAggregator { get; set; }

        [ImportingConstructor]
        public MediaStackPanelView(IEventAggregator eventAggregator)
        {
            InitializeComponent();

            EventAggregator = eventAggregator;

            itemsControl.Height = itemsControl.Height + SystemParameters.HorizontalScrollBarHeight;

            eventAggregator.GetEvent<MediaBrowserDisplayEvent>().Subscribe(imageStackPanelView_DisplayEvent, ThreadOption.UIThread);
            eventAggregator.GetEvent<MediaSelectionEvent>().Subscribe(selectItem, ThreadOption.UIThread);

            scrollToIndex = -1;           
                      
        }

        private void imageStackPanelView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MediaStackPanelViewModel)
            {
                MediaStackPanelViewModel imageStackPanelViewModel = e.OldValue as MediaStackPanelViewModel;                                        
            }

            if (e.NewValue is MediaStackPanelViewModel)
            {
                MediaStackPanelViewModel imageStackPanelViewModel = e.NewValue as MediaStackPanelViewModel;                                                                                                           
            }
        }
     
        private void scrollViewer_Loaded(object sender, EventArgs e)
        {
            scrollViewer = (ScrollViewer)sender;

            if (scrollToIndex != -1)
            {
                scrollViewer.ScrollToHorizontalOffset(scrollToIndex);
                
            }
         
        }

        private void imageStackPanelView_DisplayEvent(MediaBrowserDisplayOptions options)
        {
            if (options.IsHidden != null)
            {

                if (options.IsHidden == true)
                {
                    if (collapseToggleButton.IsChecked.Value == false)
                    {
                        collapseToggleButton.IsChecked = true;
                    }
                }
                else
                {
                    if (collapseToggleButton.IsChecked.Value == true)
                    {
                        collapseToggleButton.IsChecked = false;
                    }
                }
            }

            if (options.IsEnabled != null)
            {
                if (options.IsEnabled == false)
                {
                    collapseToggleButton.IsChecked = true;
                    collapseToggleButton.Visibility = Visibility.Collapsed;
                }
                else
                {
                    collapseToggleButton.Visibility = Visibility.Visible;
                }                               
            } 

            if (options.FilterMode != null)
            {
                ViewModel.MediaStateCollectionView.FilterModes.MoveCurrentTo(options.FilterMode);
            }

            if (options.SelectedItem != null)
            {
                selectItem(options.SelectedItem);
                
            }

            //ViewModel.FilterMode = options.FilterMode;
        }

        void selectItem(MediaFileItem item)
        {
            ICollection<MediaFileItem> selectedItems = ViewModel.MediaStateCollectionView.getSelectedItems();
            if (selectedItems.Count > 0 && selectedItems.ElementAt(0).Equals(item)) return;

            ViewModel.MediaStateCollectionView.deselectAll();
            ViewModel.MediaStateCollectionView.setIsSelected(item, true);

            scrollToIndex = ViewModel.MediaStateCollectionView.getIndexOf(item);

            if (scrollViewer != null)
            {
                scrollViewer.ScrollToHorizontalOffset(scrollToIndex);
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return (true);
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            ViewModel = (MediaStackPanelViewModel)navigationContext.Parameters["viewModel"];

            DataContext = ViewModel;

            String location = (String)navigationContext.Parameters["location"];
            if (!String.IsNullOrEmpty(location))
            {
                selectItem(MediaFileItem.Factory.create(location));
            }
        }        
  
    }
}
