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
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Regions;
using MediaViewer.MediaFileBrowser;
using MediaViewer.ImagePanel;
using MediaViewer.VideoPanel;
using MediaViewer.Model.Media.File.Watcher;
using System.Reflection;
using Microsoft.Practices.Prism.PubSubEvents;
using MediaViewer.Model.Utils;
using MediaViewer.Logging;
using MediaViewer.Model.GlobalEvents;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace MediaViewer
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>    
    [Export]   
    public partial class Shell : Window, IPartImportsSatisfiedNotification
    {

        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [Import(AllowRecomposition = false)]
        public IRegionManager RegionManager;

        [Import(AllowRecomposition = false)]
        public IEventAggregator EventAggregator;

        public static ShellViewModel ShellViewModel { get; set; }

        System.Windows.WindowState prevWindowState;

        public Shell()
        {
           
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Application_UnhandledException);

            InitializeComponent();

            XMPLib.MetaData.setLogCallback(new XMPLib.MetaData.LogCallbackDelegate(metaData_logCallback));
         
            AppDomain currentDomain = AppDomain.CurrentDomain;
            PrintLoadedAssemblies(currentDomain);
            currentDomain.AssemblyLoad += new AssemblyLoadEventHandler(assemblyLoadEventHandler);

            this.Closing += Shell_Closing;
        }

        public void OnImportsSatisfied()
        {
            ShellViewModel = new ShellViewModel(MediaFileWatcher.Instance, RegionManager, EventAggregator);

            DataContext = ShellViewModel;

            this.RegionManager.RegisterViewWithRegion(RegionNames.MainNavigationToolBarRegion, typeof(ImageNavigationItemView));
            this.RegionManager.RegisterViewWithRegion(RegionNames.MainNavigationToolBarRegion, typeof(VideoNavigationItemView));
            this.RegionManager.RegisterViewWithRegion(RegionNames.MainNavigationToolBarRegion, typeof(MediaFileBrowserNavigationItemView));

            EventAggregator.GetEvent<TitleChangedEvent>().Subscribe((title) =>
            {
                if (!String.IsNullOrEmpty(title))
                {
                    this.Title = "MediaViewer - " + title;
                }
                else
                {
                    this.Title = "MediaViewer";
                }

            },ThreadOption.UIThread);

            EventAggregator.GetEvent<ToggleFullScreenEvent>().Subscribe((isFullscreen) =>
                {
                    if (isFullscreen)
                    {
                        prevWindowState = WindowState;
                        WindowState = System.Windows.WindowState.Maximized;
                        WindowStyle = System.Windows.WindowStyle.None;
                        toolBarPanel.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        WindowState = prevWindowState;
                        WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                        toolBarPanel.Visibility = Visibility.Visible;
                    }
                }, ThreadOption.UIThread);


            try {

                String location = App.Args.Count() > 0 ? FileUtils.getProperFilePathCapitalization(App.Args[0]) : "";

                if (MediaViewer.Model.Utils.MediaFormatConvert.isImageFile(location))
                {
                    MediaFileWatcher.Instance.Path = FileUtils.getPathWithoutFileName(location);
                    ShellViewModel.navigateToImageView(location);

                }
                else if (MediaFormatConvert.isVideoFile(location))
                {
                    MediaFileWatcher.Instance.Path = FileUtils.getPathWithoutFileName(location);
                    ShellViewModel.navigateToVideoView(location);
                }
                else
                {
                    ShellViewModel.navigateToMediaFileBrowser();
                }

            } catch (Exception e) {

                log.Error("Error in command line argument: " + App.Args[0], e);
                ShellViewModel.navigateToMediaFileBrowser();
            }
                  
        }

        private void metaData_logCallback(XMPLib.MetaData.LogLevel level, string message)
        {

            switch (level)
            {

                case XMPLib.MetaData.LogLevel.ERROR:
                    {
                        log.Error(message);
                        break;
                    }
                case XMPLib.MetaData.LogLevel.WARNING:
                    {

                        log.Warn(message);
                        break;
                    }
                case XMPLib.MetaData.LogLevel.INFO:
                    {

                        log.Info(message);
                        break;
                    }
                default:
                    {
                        System.Diagnostics.Debug.Assert(false);
                        break;
                    }
            }
        }

        private void Shell_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MediaViewer.Settings.AppSettings.Instance.save();
            Dispatcher.InvokeShutdown();
        }

        private void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            log.Error("Unhandled exception: " + e.ExceptionObject.ToString() + " Terminating: " + e.IsTerminating.ToString());
        }

        void PrintLoadedAssemblies(AppDomain domain)
        {
            foreach (Assembly a in domain.GetAssemblies())
            {
                log.Info("Assembly loaded: " + a.FullName);
            }
        }

        void assemblyLoadEventHandler(object sender, AssemblyLoadEventArgs args)
        {
            log.Info("Assembly loaded: " + args.LoadedAssembly.FullName);
        }
    }
}
