﻿using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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

namespace GoogleImageSearchPlugin
{
    /// <summary>
    /// Interaction logic for GoogleImageSearchNavigationItem.xaml
    /// </summary>
    [Export]
    [ViewSortHint("04")]
    public partial class GoogleImageSearchNavigationItemView : UserControl
    {
        
        public GoogleImageSearchNavigationItemView()
        {
            InitializeComponent();
        }

        private void navigationButton_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;

            GoogleImageSearchView imageSearch = new GoogleImageSearchView();
            imageSearch.DataContext = new GoogleImageSearchViewModel();
            imageSearch.Closed += imageSearch_Closed;

            imageSearch.Show();
        }

        void imageSearch_Closed(object sender, EventArgs e)
        {
            this.IsEnabled = true;
            navigationButton.IsChecked = false;
        }
    }
}
