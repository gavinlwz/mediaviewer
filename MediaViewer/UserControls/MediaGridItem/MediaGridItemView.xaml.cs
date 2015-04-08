﻿using MediaViewer.MediaDatabase;
using MediaViewer.Model.Global.Events;
using MediaViewer.Model.Media.Base;
using MediaViewer.Model.Media.File;
using MediaViewer.Model.Media.State.CollectionView;
using MediaViewer.Model.Utils;
using Microsoft.Practices.Prism.PubSubEvents;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MediaViewer.UserControls.MediaGridItem
{
    /// <summary>
    /// Interaction logic for MediaGridItemView.xaml
    /// </summary>
    public partial class MediaGridItemView : UserControl
    {
        public event EventHandler<SelectableMediaItem> Click;

        static InfoIconsCache InfoIconsCache { get; set; }     
        static IEventAggregator EventAggregator { get; set; }

        public MediaGridItemView()
        {
            InitializeComponent();

            if(InfoIconsCache == null) {                

                InfoIconsCache = new InfoIconsCache();              

                EventAggregator = ServiceLocator.Current.GetInstance(typeof(IEventAggregator)) as IEventAggregator;
            }

            Unloaded += (s, e) =>
            {
                // make sure the propertychanged even is removed in case the mediaitem is not garbage collected
                // to prevent it being attached multiple times
                WeakEventManager<MediaItem, PropertyChangedEventArgs>.RemoveHandler(SelectableMediaItem.Item, "PropertyChanged", mediaItem_PropertyChanged);
            };
        }

        public SelectableMediaItem SelectableMediaItem
        {
            get { return (SelectableMediaItem)GetValue(SelectableMediaItemProperty); }
            set { SetValue(SelectableMediaItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectableMediaFileItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectableMediaItemProperty =
            DependencyProperty.Register("SelectableMediaItem", typeof(SelectableMediaItem), typeof(MediaGridItemView), new PropertyMetadata(null,selectableMediaItemChanged));

        private static void selectableMediaItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MediaGridItemView view = d as MediaGridItemView;

            if (e.NewValue != null)
            {
                MediaItem item = (e.NewValue as SelectableMediaItem).Item;

                WeakEventManager<MediaItem, PropertyChangedEventArgs>.AddHandler(item, "PropertyChanged", view.mediaItem_PropertyChanged);
                view.infoIconsImage.Source = InfoIconsCache.getInfoIconsBitmap(item);

                if (view.MediaStateCollectionView != null)
                {
                    view.setExtraInfo();
                }
            }
        }
       
        private void mediaItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => {
                
                infoIconsImage.Source = InfoIconsCache.getInfoIconsBitmap(SelectableMediaItem.Item);
            }));
        }

        public MediaStateCollectionView MediaStateCollectionView
        {
            get { return (MediaStateCollectionView)GetValue(MediaStateCollectionViewProperty); }
            set { SetValue(MediaStateCollectionViewProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MediaStateCollectionView.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MediaStateCollectionViewProperty =
            DependencyProperty.Register("MediaStateCollectionView", typeof(MediaStateCollectionView), typeof(MediaGridItemView), new PropertyMetadata(null, mediaStateCollectionViewChanged));

        private static void mediaStateCollectionViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                MediaGridItemView view = (MediaGridItemView)d;

                if (view.SelectableMediaItem != null)
                {
                    view.setExtraInfo();
                }
            }
        }

        void setExtraInfo()
        {
            Object info = MediaStateCollectionView.getExtraInfo(SelectableMediaItem);
                       
            if (info is String)
            {
                extraInfoTextBlock.Text = (String)info;                
                extraInfoTextBlock.ToolTip = (String)info;
              
                extraInfoTextBlock.Visibility = System.Windows.Visibility.Visible;
                ratingImage.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                ratingImage.Source = (System.Windows.Media.ImageSource)info;

                ratingImage.Visibility = System.Windows.Visibility.Visible;
                extraInfoTextBlock.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void mediaGridItem_Click(object sender, RoutedEventArgs e)
        {
            if (Click != null)
            {
                Click(this, SelectableMediaItem);
            }
            
        }
     
    }
}
