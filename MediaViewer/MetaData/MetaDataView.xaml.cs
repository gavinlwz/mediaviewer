﻿using MediaViewer.Model.Media.File;
using MediaViewer.Model.Media.File.Watcher;
using MediaViewer.MetaData.Tree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using MediaViewer.ExtensionMethods;
using System.Windows.Threading;
using System.ComponentModel.Composition;
using MediaViewer.Model.Settings;
using Microsoft.Practices.Prism.Regions;
using Microsoft.Practices.Prism.PubSubEvents;
using MediaViewer.UserControls.Layout;
using MediaViewer.UserControls.TabbedExpander;
using Microsoft.Windows.Themes;

namespace MediaViewer.MetaData
{
    /// <summary>
    /// Interaction logic for MetaDataView.xaml
    /// </summary>
    [Export]
    public partial class MetaDataView : UserControl, IRegionMemberLifetime, INavigationAware, ITabbedExpanderAware
    {

        MetaDataViewModel MetaDataViewModel { get; set; }     
        
        [ImportingConstructor] 
        public MetaDataView(IEventAggregator eventAggregator)
        {
            InitializeComponent();

            MetaDataViewModel = new MetaDataViewModel(MediaFileWatcher.Instance, eventAggregator);
                                                
            DataContext = MetaDataViewModel;

            TabName = "Metadata";
            TabIsSelected = true;
            TabMargin = new Thickness(2);
            TabBorderThickness = new Thickness(2);
            TabBorderBrush = ClassicBorderDecorator.ClassicBorderBrush;
        }

        void insertStringInReplaceFilename(String str)
        {
            TextBox textBox = replaceTextBox.getChildrenOfType<TextBox>().
                       FirstOrDefault(element => element.Name == "PART_EditableTextBox");

            if (textBox == null)
            {
                return;
            }

            int index = textBox.CaretIndex;

            MetaDataViewModel.ReplaceFilename = MetaDataViewModel.ReplaceFilename.Insert(index, str);

        }
        
        private void fileNameContextMenu_InsertCounter(object sender, RoutedEventArgs e)
        {
            insertStringInReplaceFilename(MetaDataUpdateViewModel.counterMarker);
        }

        private void fileNameContextMenu_InsertParentDir(object sender, RoutedEventArgs e)
        {
            insertStringInReplaceFilename(MetaDataUpdateViewModel.parentDirMarker);
        }

        private void fileNameContextMenu_InsertWidth(object sender, RoutedEventArgs e)
        {
            insertStringInReplaceFilename(MetaDataUpdateViewModel.widthMarker);
        }

        private void fileNameContextMenu_InsertHeight(object sender, RoutedEventArgs e)
        {
            insertStringInReplaceFilename(MetaDataUpdateViewModel.heightMarker);
        }

        private void fileNameContextMenu_InsertBitrate(object sender, RoutedEventArgs e)
        {
            insertStringInReplaceFilename(MetaDataUpdateViewModel.bitrateMarker);
        }

        private void fileNameContextMenu_InsertDate(object sender, RoutedEventArgs e)
        {
            insertStringInReplaceFilename(MetaDataUpdateViewModel.dateMarker);

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
            MetaDataViewModel.OnNavigatedFrom(navigationContext);           
        }        

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            MetaDataViewModel.OnNavigatedTo(navigationContext);            
        }

        public string TabName { get; set; }
        public bool TabIsSelected { get; set; }
        public Thickness TabMargin { get; set; }
        public Thickness TabBorderThickness { get; set; }
        public Brush TabBorderBrush { get; set; }
    }
}
