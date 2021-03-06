﻿//http://bjorn.kuiper.nu/2011/05/11/tips-tricks-listening-to-dependency-property-change-notifications-of-a-given-element/
using Microsoft.Practices.Prism.Mvvm;
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
using Microsoft.Practices.Prism.Regions;
using MediaViewer.Model.Media.File;
using MediaViewer.MediaFileBrowser;
using Microsoft.Practices.Prism.PubSubEvents;
using MediaViewer.Model.Global.Events;
using MediaViewer.Infrastructure.Global.Events;

namespace MediaViewer.ImagePanel
{
    /// <summary>
    /// Interaction logic for ImageView.xaml
    /// </summary>
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class ImageView : UserControl, IRegionMemberLifetime, INavigationAware
    {
        bool IsFullScreen { get; set; }

        ImageViewModel ViewModel { get; set; }
        IEventAggregator EventAggregator { get; set; }

        [ImportingConstructor]
        public ImageView(IEventAggregator eventAggregator)
        {
            EventAggregator = eventAggregator;

            InitializeComponent();
                     
            IsFullScreen = false;
           
        }

        private void imagePanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                EventAggregator.GetEvent<ToggleFullScreenEvent>().Publish(IsFullScreen = !IsFullScreen);
            }
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
            ViewModel.OnNavigatedFrom(navigationContext);            
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            ViewModel = (ImageViewModel)navigationContext.Parameters["viewModel"];
            DataContext = ViewModel;

            ViewModel.OnNavigatedTo(navigationContext);              
        }       
    }
}
