﻿using MediaViewer.MediaDatabase;
using MediaViewer.MediaDatabase.DbCommands;
using MediaViewer.UserControls.TagTreePicker;
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
using System.Windows.Shapes;

namespace MediaViewer.MetaData
{
    /// <summary>
    /// Interaction logic for LinkedTagEditorView.xaml
    /// </summary>
    public partial class TagEditorView : Window
    {
        TagEditorViewModel tagEditorViewModel;

        public TagEditorView()
        {
            InitializeComponent();
            DataContext = tagEditorViewModel = new TagEditorViewModel();

            tagEditorViewModel.ClosingRequest += new EventHandler<MvvmFoundation.Wpf.CloseableObservableObject.DialogEventArgs>((s, e) =>
            {                
                this.Close();
            });

            tagEditorViewModel.ImportCommand.Executed += new MvvmFoundation.Wpf.Delegates.CommandEventHandler((s,e) =>
            {
                tagTreePicker.reloadAll();
            });
        }

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            tagTreePicker.unregisterMessages();          
        }
       
    }
}
