﻿using MediaViewer.Model.GlobalEvents;
using MediaViewer.MediaGrid;
using MediaViewer.MediaFileBrowser;
using MediaViewer.Model.Media.File;
using Microsoft.Practices.Prism.PubSubEvents;
using Microsoft.Practices.Prism.Regions;
using MvvmFoundation.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
using MediaViewer.Model.Utils;
using MediaViewer.UserControls.VideoSlider;

namespace MediaViewer.VideoPanel
{
    /// <summary>
    /// Interaction logic for VideoView.xaml
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class VideoView : UserControl, IRegionMemberLifetime, INavigationAware
    {

        bool updateTimeLineSlider;


        public VideoViewModel ViewModel { get; set; }
      
        IEventAggregator EventAggregator {get;set;}

        [ImportingConstructor]
        public VideoView(IEventAggregator eventAggregator)
        {
            InitializeComponent();

            timeLineSlider.Maximum = 0;

            updateTimeLineSlider = true;

            EventAggregator = eventAggregator;

            EventAggregator.GetEvent<ToggleFullScreenEvent>().Subscribe((isFullScreen) =>
            {
                if (isFullScreen)
                {
                    uiGrid.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    uiGrid.Visibility = System.Windows.Visibility.Visible;
                }

            });
        }

        void VideoView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is VideoViewModel)
            {
                timeLineSlider.RemoveHandler(Slider.MouseLeftButtonDownEvent, new MouseButtonEventHandler(timeLineSlider_MouseLeftButtonDownEvent));
                timeLineSlider.RemoveHandler(Slider.MouseLeftButtonUpEvent, new MouseButtonEventHandler(timeLineSlider_MouseLeftButtonUpEvent));

                VideoViewModel viewModel = e.OldValue as VideoViewModel;

                videoPlayer.DoubleClick -= videoPlayer_DoubleClick;
                viewModel.PropertyChanged -= videoPlayerViewModel_PropertyChanged;

                viewModel.Dispose();
            }

            if (e.NewValue is VideoViewModel)
            {
                VideoViewModel viewModel = e.NewValue as VideoViewModel;

                viewModel.initializeVideoPlayer(videoPlayer.ViewModel);

                videoPlayer.DoubleClick += videoPlayer_DoubleClick;
                viewModel.PropertyChanged += videoPlayerViewModel_PropertyChanged;

                timeLineSlider.AddHandler(Slider.MouseLeftButtonDownEvent, new MouseButtonEventHandler(timeLineSlider_MouseLeftButtonDownEvent),
                   true);
                timeLineSlider.AddHandler(Slider.MouseLeftButtonUpEvent, new MouseButtonEventHandler(timeLineSlider_MouseLeftButtonUpEvent),
                  true);

            }

        }

        private void videoPlayerViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("PositionSeconds") && updateTimeLineSlider == true)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(updateTimeLine));
            }
            else if (e.PropertyName.Equals("VideoState"))
            {

                switch (ViewModel.VideoState)
                {
                    case VideoPlayerControl.VideoState.PLAYING:
                        {
                            if (playButton.IsChecked == false)
                            {
                                playButton.IsChecked = true;
                            }

                            break;
                        }
                    case VideoPlayerControl.VideoState.PAUSED:
                        {
                            if (playButton.IsChecked == true)
                            {
                                playButton.IsChecked = false;
                            }

                            break;
                        }
                    case VideoPlayerControl.VideoState.CLOSED:
                        {
                            if (playButton.IsChecked == true)
                            {
                                playButton.IsChecked = false;
                            }

                            break;
                        }

                }
            }
        }

        private void videoPlayer_DoubleClick(object sender, EventArgs e)
        {
            if (uiGrid.Visibility == Visibility.Visible)
            {
                EventAggregator.GetEvent<ToggleFullScreenEvent>().Publish(true);
            }
            else
            {
                EventAggregator.GetEvent<ToggleFullScreenEvent>().Publish(false);
            }
            
        }

        public void openAndPlay(String location)
        {
            try
            {
                ViewModel.OpenCommand.DoExecute(location);
                ViewModel.PlayCommand.DoExecute();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void updateTimeLine()
        {
            timeLineSlider.Value = ViewModel.PositionSeconds;
            timeLineSlider.Maximum = ViewModel.DurationSeconds;
        }

        

        private void timeLineSlider_MouseLeftButtonDownEvent(object sender, MouseButtonEventArgs e)
        {
            updateTimeLineSlider = false;

        }

        private void timeLineSlider_MouseLeftButtonUpEvent(object sender, MouseButtonEventArgs e)
        {

            VideoSliderView slider = sender as VideoSliderView;
            Point position = e.GetPosition(slider);
            double d = 1.0d / slider.ActualWidth * position.X;
            var p = slider.Maximum * d;

            int sliderValue = (int)p;

            ViewModel.SeekCommand.DoExecute(sliderValue);

            updateTimeLineSlider = true;
        }

        private void playButton_Checked(object sender, RoutedEventArgs e)
        {
            ViewModel.PlayCommand.DoExecute();
        }

        private void playButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.PauseCommand.DoExecute();
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
            if(navigationContext.Uri.Equals(new Uri(typeof(MediaGridView).FullName,UriKind.Relative))) {

                ViewModel.CloseCommand.DoExecute();
            }

            EventAggregator.GetEvent<MediaSelectionEvent>().Unsubscribe(videoView_MediaSelectionEvent);
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            ViewModel = (VideoViewModel)navigationContext.Parameters["viewModel"];
            DataContext = ViewModel;
            
            String location = (String)navigationContext.Parameters["location"];
           
            if (!String.IsNullOrEmpty(location))
            {
                ViewModel.OpenCommand.DoExecute(location);
                ViewModel.PlayCommand.DoExecute();

            } else {
             
                EventAggregator.GetEvent<TitleChangedEvent>().Publish(ViewModel.CurrentLocation == null ? "" : ViewModel.CurrentLocation);                
            }

            EventAggregator.GetEvent<MediaSelectionEvent>().Subscribe(videoView_MediaSelectionEvent, ThreadOption.UIThread);
            
        }

        private void videoView_MediaSelectionEvent(MediaFileItem item)
        {          
            if (String.Equals(ViewModel.CurrentLocation,item.Location)) return;

            ViewModel.OpenCommand.DoExecute(item.Location);
            ViewModel.PlayCommand.DoExecute();            
        }
    }
}
