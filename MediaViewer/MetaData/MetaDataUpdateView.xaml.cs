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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MediaViewer.MetaData
{
    /// <summary>
    /// Interaction logic for MetaDataUpdateView.xaml
    /// </summary>
    public partial class MetaDataUpdateView : Window
    {
        public MetaDataUpdateView()
        {
            InitializeComponent();
            MetaDataUpdateViewModel vm = new MetaDataUpdateViewModel();
            DataContext = vm;

            vm.ClosingRequest += new EventHandler((o, e) =>
            {               
                this.Close();               
            });

            Closing += new System.ComponentModel.CancelEventHandler((s, e) =>
            {
                vm.CancelCommand.DoExecute();
            });
        }
    }
}
