﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MvvmFoundation.Wpf;
using System.Windows;
using MediaViewer.Utils;
using MediaViewer.MetaData;
using MediaViewer.About;
using MediaViewer.Logging;

namespace MediaViewer
{
    class MainWindowViewModel : ObservableObject
    {
        protected static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        string currentImageLocation;

        public string CurrentImageLocation
        {
            get { return currentImageLocation; }
            set
            {
                currentImageLocation = value;
                NotifyPropertyChanged();
            }
        }
        string currentVideoLocation;

        public string CurrentVideoLocation
        {
            get { return currentVideoLocation; }
            set
            {
                currentVideoLocation = value;
                NotifyPropertyChanged();
            }
        }

        string windowTitle;

        public string WindowTitle
        {
            get { return windowTitle; }
            set { windowTitle = value;
            NotifyPropertyChanged();
            }
        }

        public MainWindowViewModel()
        {

            WindowTitle = "MediaViewer";

            // recieve messages requesting the display of media items

            GlobalMessenger.Instance.Register<string>("MainWindowViewModel.ViewMediaCommand", new Action<string>((fileName) =>
            {
                ViewMediaCommand.DoExecute(fileName);
            }));

            ViewMediaCommand = new Command(new Action<object>((location) =>
              {
                  String mimeType = MediaFormatConvert.fileNameToMimeType((string)location);

                  if (mimeType.StartsWith("image"))
                  {
                      CurrentImageLocation = (string)location;
                  }
                  else if (mimeType.StartsWith("video"))
                  {
                      CurrentVideoLocation = (string)location;
                  }
                  else
                  {
                      log.Warn("Trying to view media of unknown mime type: " + (string)location + ", mime type: " + mimeType);
                  }

              }));

            TagEditorCommand = new Command(new Action(() =>
                {
                    LinkedTagEditorView linkedTagEditor = new LinkedTagEditorView();
                    linkedTagEditor.ShowDialog();
                }));

            AboutCommand = new Command(new Action(() =>
                {
                    AboutView about = new AboutView();
                    AboutViewModel aboutViewModel = new AboutViewModel();
                    about.DataContext = aboutViewModel;

                    about.ShowDialog();

                }));

            ShowLogCommand = new Command(new Action(() =>
                {
                    log4net.Appender.IAppender[] appenders = log4net.LogManager.GetRepository().GetAppenders();
                    VisualAppender appender = (VisualAppender)(appenders[0]);

                    LogView logView = new LogView();
                    logView.DataContext = appender.LogViewModel;

                    logView.Show();

                }));
        }

        Command viewMediaCommand;

        public Command ViewMediaCommand
        {
            get { return viewMediaCommand; }
            set { viewMediaCommand = value; }
        }

        Command tagEditorCommand;

        public Command TagEditorCommand
        {
            get { return tagEditorCommand; }
            set { tagEditorCommand = value; }
        }

        Command aboutCommand;

        public Command AboutCommand
        {
            get { return aboutCommand; }
            set { aboutCommand = value; }
        }

        Command showLogCommand;

        public Command ShowLogCommand
        {
            get { return showLogCommand; }
            set { showLogCommand = value; }
        }
    }
}
