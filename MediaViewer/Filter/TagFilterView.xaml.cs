﻿//https://diptimayapatra.wordpress.com/2010/03/05/grouping-in-datagrid-in-wpf/
using Aga.Controls.Tree;
using MediaViewer.MediaDatabase;
using MediaViewer.Model.Collections;
using MediaViewer.Model.Collections.Sort;
using MediaViewer.Model.Global.Events;
using MediaViewer.Model.Media.Base;
using MediaViewer.Model.Media.File;
using MediaViewer.Model.Media.State;
using MediaViewer.Model.Media.State.CollectionView;
using MediaViewer.Model.Utils;
using MediaViewer.Model.Utils.WPF;
using MediaViewer.TagEditor;
using MediaViewer.UserControls.Layout;
using Microsoft.Practices.Prism.PubSubEvents;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MediaViewer.Filter
{
    [Export]
    public partial class TagFilterView : UserControl, IRegionMemberLifetime, INavigationAware, IExpanderPanelAware
    {
        TagItemList tagsList;

        IEventAggregator EventAggregator { get; set; }        
        bool extendTimer;
        
        Timer timer;
        int timerInterval = 100;

        [ImportingConstructor]
        public TagFilterView(IEventAggregator eventAggregator)
        {
            InitializeComponent();
            EventAggregator = eventAggregator;
            
            EventAggregator.GetEvent<MediaBrowserActiveMediaStateCollectionViewChangedEvent>().Subscribe(activeMediaStateCollectionViewChanged);

            timer = new Timer();
            timer.AutoReset = false;
            timer.Interval = timerInterval;
            timer.Elapsed += timer_Elapsed;

            extendTimer = false;

            Header = "Tag Filter";
            ElementHeight = 1;
            IsAddBorder = true;
            IsIntiallyExpanded = false;

            tagsList = new TagItemList();
            tagsList.IsFilterChanged += tagsList_IsFilterChanged;
            dataGrid.ItemsSource = tagsList;

            ICollectionView view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
            view.SortDescriptions.Add(new SortDescription("Count", ListSortDirection.Descending));
            dataGrid.ColumnFromDisplayIndex(1).SortDirection = ListSortDirection.Descending;
        }

        private void activeMediaStateCollectionViewChanged(MediaFileStateCollectionView newState)
        {
            MediaCollectionView = newState;
        }

        MediaFileStateCollectionView mediaCollectionView;

        public MediaFileStateCollectionView MediaCollectionView
        {
            get { return (mediaCollectionView); }
            set {

                if (ReferenceEquals(value, mediaCollectionView))
                {
                    return;
                }

                if (mediaCollectionView != null)
                {
                    mediaCollectionView.ItemPropertyChanged -= mediaCollection_ItemPropertyChanged;
                    mediaCollectionView.Media.CollectionChanged -= mediaCollection_CollectionChanged;
                    mediaCollectionView.Cleared -= mediaCollectionView_Cleared;
                }
                
                if (value != null)
                {
                    value.ItemPropertyChanged += mediaCollection_ItemPropertyChanged;
                    value.Media.CollectionChanged += mediaCollection_CollectionChanged;
                    value.Cleared += mediaCollectionView_Cleared;
                }

                mediaCollectionView = value;

                buildTagList();
               
            }
        }

        void mediaCollectionView_Cleared(object sender, EventArgs e)
        {
            mediaCollectionView.TagFilter.Clear();
        }

        private void mediaCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                App.Current.Dispatcher.BeginInvoke(new Action(tagsList.Clear));                                                       
            }

            if (timer.Enabled) 
            {
                extendTimer = true;
            }
            else 
            {                
                timer.Enabled = true;
            }
        }

        private void mediaCollection_ItemPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (timer.Enabled)
            {
                extendTimer = true;
            } 
            else 
            {                           
                timer.Enabled = true;
            }
            
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (extendTimer)
            {
                //System.Diagnostics.Debug.Print("Extend timer " + System.Threading.Thread.CurrentThread.ManagedThreadId);
                extendTimer = false;
                timer.Enabled = true;                
                return;
            }

            //System.Diagnostics.Debug.Print("Stop timer " + System.Threading.Thread.CurrentThread.ManagedThreadId);
            buildTagList();

            
        }

        void tagsList_IsFilterChanged(object sender, EventArgs e)
        {
            ToggleButton clearToggleButton = 
                VisualTreeUtils.findVisualChildByName<ToggleButton>(dataGrid, "clearToggleButton");
            
            TagItem item = (TagItem)sender;

            if (item.IsFilter)
            {
                mediaCollectionView.TagFilter.Add(item);
                clearToggleButton.IsEnabled = true;
                clearToggleButton.IsChecked = true;
            }
            else
            {
                mediaCollectionView.TagFilter.Remove(item);

                if (mediaCollectionView.TagFilter.Count == 0)
                {
                    clearToggleButton.IsEnabled = false;
                    clearToggleButton.IsChecked = false;
                }
            }

            MediaCollectionView.refresh();
        }

        void addTags(SelectableMediaItem item, List<TagItem> tags)
        {
            if (item.Item.Metadata == null) return;

            item.Item.RWLock.EnterReadLock();
            try
            {                
                foreach (Tag tag in item.Item.Metadata.Tags)
                {
                    TagItem tagItem = new TagItem(tag, mediaCollectionView);

                    int index = tags.IndexOf(tagItem);

                    if (index == -1)
                    {
                        tags.Add(tagItem);
                    }
                    else
                    {
                        tags[index].Count += 1;
                    }
                }
            }
            finally
            {
                item.Item.RWLock.ExitReadLock();
            }
        }

        void buildTagList()
        {
            List<TagItem> tagItems = new List<TagItem>();            

            MediaCollectionView.Media.EnterReadLock();
            try
            {
                foreach (SelectableMediaItem media in MediaCollectionView.Media)
                {
                    if (media.Item.ItemState == MediaItemState.LOADED)
                    {
                        addTags(media, tagItems);                        
                    }
                }               
            }
            finally
            {
                MediaCollectionView.Media.ExitReadLock();
            }

            App.Current.Dispatcher.BeginInvoke(new Action(() => {

                tagsList.Clear();
                tagsList.AddRange(tagItems);               

                ICollectionView view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
                view.Refresh();

            }));

/*          
            App.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                SortDescription? sortDescription = null;

                ICollectionView view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
                if (view != null && view.SortDescriptions.Count > 0)
                {
                    sortDescription = view.SortDescriptions[0];                    
                }

                dataGrid.ItemsSource = tagsList.tagItems;

                view = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
                if (sortDescription == null)
                {
                    sortDescription = new SortDescription("Count", ListSortDirection.Descending);
                }
                
                view.SortDescriptions.Add(sortDescription.Value);
                int index = dataGridColumnIndex(sortDescription.Value.PropertyName);

                dataGrid.ColumnFromDisplayIndex(index).SortDirection = sortDescription.Value.Direction;

                view.Refresh();

            }));*/
        }

        int dataGridColumnIndex(string propertyName)
        {
            int i = 0;

            foreach (DataGridColumn column in dataGrid.Columns)
            {
                if (column.Header.Equals(propertyName))
                {
                    return (i);
                }

                i++;
            }

            return (-1);
        }

        public bool KeepAlive
        {
            get { return (true); }
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
            MediaCollectionView = (MediaFileStateCollectionView)navigationContext.Parameters["MediaStateCollectionView"];
        }

        public string Header { get; set; }
        public int ElementHeight { get; set; }
        public bool IsAddBorder { get; set; }
        public bool IsIntiallyExpanded { get; set; }

      
        private void clearToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (TagItem item in tagsList)
            {
                item.IsFilter = false;
            }
        }
    }
}
