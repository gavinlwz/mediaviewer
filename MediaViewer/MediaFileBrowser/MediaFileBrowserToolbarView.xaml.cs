﻿using System;
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
using Microsoft.Practices.Prism.Regions;

namespace MediaViewer.MediaFileBrowser
{
    /// <summary>
    /// Interaction logic for ImageGridToolbarView.xaml
    /// </summary>
    [Export]
    public partial class MediaFileBrowserToolbarView : UserControl, INavigationAware
    {
              
        public MediaFileBrowserToolbarView()
        {
            InitializeComponent();         
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
            DataContext = navigationContext.Parameters["viewModel"];
        }
    }
}
