﻿// intresting stuff:
// Lazy<T> for lazy initialization
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MediaViewer.MediaPreview;
using MediaViewer.Utils.WPF;

namespace MediaViewer.ImageGrid
{
    /// <summary>
    /// Interaction logic for ImageGridControl.xaml
    /// </summary>
    public partial class ImageGridView : UserControl
    {


        public ImageGridView()
        {
           
            InitializeComponent();
         
                      
        }
        

        private void viewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)e.OriginalSource;
           

            GlobalMessenger.Instance.NotifyColleagues("MainWindowViewModel.ViewMediaCommand", item.Tag);
           
        }

        private void selectAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ImageGridViewModel vm = (ImageGridViewModel)DataContext;

            vm.selectAll();
        }

        private void deselectAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ImageGridViewModel vm = (ImageGridViewModel)DataContext;

            vm.deselectAll();

        }

       
    }
}
