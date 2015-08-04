﻿using MediaViewer.Infrastructure.Global.Events;
using MediaViewer.Model.Global.Events;
using MediaViewer.Model.Media.Base;
using MediaViewer.Model.Mvvm;
using MediaViewer.Model.Utils;
using Microsoft.Practices.Prism.Mvvm;
using Microsoft.Practices.Prism.PubSubEvents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.MediaFileBrowser.ImagePanel
{
    public class MediaFileBrowserImagePanelViewModel : BindableBase, IMediaFileBrowserContentViewModel
    {
        
        IEventAggregator EventAggregator { get; set; }

        public MediaFileBrowserImagePanelViewModel(IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;
        
            isLoading = false;

            Rotate90DegreesCommand = new Command(() =>
            {
                RotationDegrees += 90;
            });

            RotateMinus90DegreesCommand = new Command(() =>
            {
                RotationDegrees -= 90;
            });
  
            SelectedScaleMode = UserControls.ImagePanel.ScaleMode.UNSCALED;
  
            SetNormalScaleCommand = new Command(() =>
            {               
                SelectedScaleMode = UserControls.ImagePanel.ScaleMode.UNSCALED;
            });

            SetBestFitScaleCommand = new Command(() =>
            {      
                SelectedScaleMode = UserControls.ImagePanel.ScaleMode.FIT_HEIGHT_AND_WIDTH;
            });
   
         
        }

        bool flipX;

        public bool FlipX
        {
            get { return flipX; }
            set
            {              
                SetProperty(ref flipX, value);
             
            }
        }

        bool flipY;

        public bool FlipY
        {
            get { return flipY; }
            set
            {            
                SetProperty(ref flipY, value);          
            }
        }

        bool isLoading;

        public bool IsLoading
        {
            get { return isLoading; }
            set
            {               
                SetProperty(ref isLoading, value);
            }
        }

        double rotationDegrees;

        public double RotationDegrees
        {
            get { return rotationDegrees; }
            set
            {
                double tempRotationDegrees = value;

                if (tempRotationDegrees < 0)
                {
                    tempRotationDegrees = 360 + (tempRotationDegrees + Math.Floor(tempRotationDegrees / 360) * 360);
                }
                else if (tempRotationDegrees >= 360)
                {
                    tempRotationDegrees = tempRotationDegrees - Math.Floor(tempRotationDegrees / 360) * 360;
                }

                SetProperty(ref rotationDegrees, tempRotationDegrees);
           
            }
        }
          
        public Command Rotate90DegreesCommand {get; set;}
        public Command RotateMinus90DegreesCommand { get; set; }
        public Command SetBestFitScaleCommand { get; set; }
        public Command SetNormalScaleCommand { get; set; }

        double scale;

        public double Scale
        {
            get { return scale; }
            set
            {               
                SetProperty(ref scale, value);              
            }
        }

        String location;

        public String Location
        {
            get { return location; }
            private set {  
                SetProperty(ref location, value);
            }
        }

        MediaItem currentItem;

        public MediaItem CurrentItem
        {
            get { return currentItem; }
            set
            {
                if (value != null)
                {
                    Location = value.Location;
                }

                SetProperty(ref currentItem, value);
            }
        }

        MediaViewer.UserControls.ImagePanel.ScaleMode ScaleMode;

        public MediaViewer.UserControls.ImagePanel.ScaleMode SelectedScaleMode
        {
            get { return ScaleMode; }
            set
            {                                                
                SetProperty(ref ScaleMode, value);
            }
        }

        double minScale;

        public double MinScale
        {
            get { return minScale; }
            set
            {
                SetProperty(ref minScale, value);                        
            }
        }
        double maxScale;

        public double MaxScale
        {
            get { return maxScale; }
            set
            {
                SetProperty(ref maxScale, value);                         
            }
        }

        public void OnNavigatedTo(Microsoft.Practices.Prism.Regions.NavigationContext navigationContext)
        {
            MediaItem item = (MediaItem)navigationContext.Parameters["item"];

            if (item != null)
            {
                CurrentItem = item;                
            }

            EventAggregator.GetEvent<TitleChangedEvent>().Publish(CurrentItem != null ? CurrentItem.Name : null);
            
            EventAggregator.GetEvent<MediaSelectionEvent>().Subscribe(imageView_MediaSelectionEvent, ThreadOption.UIThread);
        }

        public void OnNavigatedFrom(Microsoft.Practices.Prism.Regions.NavigationContext navigationContext)
        {
            EventAggregator.GetEvent<MediaSelectionEvent>().Unsubscribe(imageView_MediaSelectionEvent);
        }

        private void imageView_MediaSelectionEvent(MediaSelectionPayload selection)
        {
            if (selection.Items.Count() == 0) return;

            MediaItem first = selection.Items.ElementAt(0);

            if (String.Equals(Location, first.Location)) return;

            CurrentItem = first;

            EventAggregator.GetEvent<TitleChangedEvent>().Publish(first.Name);
        }

        
    }
}
