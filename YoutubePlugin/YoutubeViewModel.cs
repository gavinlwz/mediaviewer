﻿using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using MediaViewer;
using MediaViewer.Filter;
using MediaViewer.Model.Media.Base;
using MediaViewer.Model.Media.File.Watcher;
using MediaViewer.Model.Media.Base.State;
using MediaViewer.Model.Media.Base.State.CollectionView;
using MediaViewer.Model.Mvvm;
using MediaViewer.Progress;
using MediaViewer.UserControls.TabbedExpander;
using Microsoft.Practices.Prism.Mvvm;
using Microsoft.Practices.Prism.PubSubEvents;
using Microsoft.Practices.Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using YoutubePlugin.Events;
using YoutubePlugin.Item;
using YoutubePlugin.Properties;
using YoutubePlugin.YoutubeChannelBrowser;
using YoutubePlugin.YoutubeMetadata;
using YoutubePlugin.YoutubeSearch;
using MediaViewer.Model.Media.Base.Item;
using MediaViewer.MediaFileStackPanel;
using MediaViewer.UserControls.MediaPreview;
using YoutubePlugin.Preview;
using MediaViewer.DirectoryPicker;

namespace YoutubePlugin
{    
    class YoutubeViewModel : BindableBase
    {
        IRegionManager RegionManager { get; set; }
        IEventAggregator EventAggregator { get; set; }
        public static YouTubeService Youtube { get; protected set; }
    
        Task SearchTask { get; set; }
        public CancellationTokenSource TokenSource { get; set; }

        static YoutubeViewModel()
        {
            Youtube = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = YoutubeApiKey.ApiKey,
                ApplicationName = "MediaViewer"
            });
        }

        public YoutubeViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            TokenSource = new CancellationTokenSource();
            
            RegionManager = regionManager;
            EventAggregator = eventAggregator;
            NrColumns = 4;

            MediaState = new MediaState();
            MediaStateCollectionView = new YoutubeCollectionView(MediaState);
            MediaState.clearUIState("Empty", DateTime.Now, MediaStateType.SearchResult);

            MediaStateCollectionView.SelectionChanged += mediaStateCollectionView_SelectionChanged;
                        
            ViewCommand = new Command<SelectableMediaItem>((selectableItem) =>
            {               
                if(selectableItem.Item.Metadata == null) return;

                if (selectableItem.Item is YoutubeVideoItem)
                {
                    YoutubeVideoItem item = selectableItem.Item as YoutubeVideoItem;

                    if (item.IsEmbeddedOnly)
                    {
                        Process.Start("https://www.youtube.com/watch?v=" + item.VideoId);
                    }
                    else
                    {
                        YoutubeVideoStreamedItem video, audio;
                        item.getStreams(out video, out audio, (int)Properties.Settings.Default.MaxPlaybackResolution);

                        Shell.ShellViewModel.navigateToVideoView(video, null, audio);
                    }
                }
                               
            });

            ViewChannelCommand = new AsyncCommand<SelectableMediaItem>(async (selectableItem) =>
            {
                if (selectableItem.Item.Metadata == null) return;

                YoutubeItem item = selectableItem.Item as YoutubeItem;

                SearchResource.ListRequest searchListRequest = Youtube.Search.List("snippet");
                searchListRequest.ChannelId = item.ChannelId;
                searchListRequest.MaxResults = YoutubeSearchViewModel.maxResults;
                searchListRequest.Order = Google.Apis.YouTube.v3.SearchResource.ListRequest.OrderEnum.Date;

                MediaStateCollectionView.FilterModes.MoveCurrentToFirst();

                await searchAsync(searchListRequest, item.ChannelTitle, false);
                
            });

            ViewPlaylistCommand = new AsyncCommand<SelectableMediaItem>(async (selectableItem) =>
                {
                    if (selectableItem.Item.Metadata == null) return;

                    YoutubePlaylistItem item = selectableItem.Item as YoutubePlaylistItem;

                    PlaylistItemsResource.ListRequest searchListRequest = Youtube.PlaylistItems.List("snippet");
                    searchListRequest.PlaylistId = item.PlaylistId;
                    searchListRequest.MaxResults = YoutubeSearchViewModel.maxResults;

                    MediaStateCollectionView.FilterModes.MoveCurrentToFirst();

                    await searchAsync(searchListRequest, item.Name, false);
                                        
                });

            SubscribeCommand = new Command<SelectableMediaItem>((selectableItem) =>
                {
                    YoutubeChannelItem item = selectableItem.Item as YoutubeChannelItem;

                    EventAggregator.GetEvent<AddFavoriteChannelEvent>().Publish(item);
                });

            DownloadCommand = new AsyncCommand<SelectableMediaItem>(async (selectableItem) => {

                List<MediaItem> items = MediaStateCollectionView.getSelectedItems();
                if (items.Count == 0)
                {
                    items.Add(selectableItem.Item);
                }

                String outputPath = null;

                switch (YoutubePlugin.Properties.Settings.Default.VideoSaveMode)
                {
                    case MediaViewer.Infrastructure.Constants.SaveLocation.Current:
                        {
                            outputPath = MediaFileWatcher.Instance.Path;
                            break;
                        }
                    case MediaViewer.Infrastructure.Constants.SaveLocation.Ask:
                        {
                            DirectoryPickerView directoryPicker = new DirectoryPickerView();
                            directoryPicker.DirectoryPickerViewModel.InfoString = "Select Output Directory";
                            directoryPicker.DirectoryPickerViewModel.SelectedPath = MediaFileWatcher.Instance.Path;

                            if (directoryPicker.ShowDialog() == false)
                            {
                                return;
                            }

                            outputPath = directoryPicker.DirectoryPickerViewModel.SelectedPath;

                            break;
                        }
                    case MediaViewer.Infrastructure.Constants.SaveLocation.Fixed:
                        {
                            outputPath = YoutubePlugin.Properties.Settings.Default.FixedDownloadPath;
                            break;
                        }
                    default:
                        break;
                }

                CancellableOperationProgressView progressView = new CancellableOperationProgressView();
                DownloadProgressViewModel vm = new DownloadProgressViewModel();
                progressView.DataContext = vm;

                progressView.Show();
                vm.OkCommand.IsExecutable = false;
                vm.CancelCommand.IsExecutable = true;

                await Task.Factory.StartNew(() =>
                {
                    vm.startDownload(outputPath, items);
                });

                vm.OkCommand.IsExecutable = true;
                vm.CancelCommand.IsExecutable = false;
                        
            });

            LoadNextPageCommand = new AsyncCommand(async () =>
            {                        
                await searchAsync(CurrentQuery, "", true);       
            });

            SelectAllCommand = new Command(() =>
            {
                MediaStateCollectionView.selectAll();
            }, false);

            DeselectAllCommand = new Command(() =>
            {
                MediaStateCollectionView.deselectAll();
            });

            ShutdownCommand = new Command(() =>
                {
                    Properties.Settings.Default.Save();
                });

            MediaState.UIMediaCollection.IsLoadingChanged += UIMediaCollection_IsLoadingChanged;

            MediaViewer.Model.Global.Commands.GlobalCommands.ShutdownCommand.RegisterCommand(ShutdownCommand);

            setupViews();

            EventAggregator.GetEvent<SearchEvent>().Subscribe(searchEvent);

            SearchTask = null;
        }

        void UIMediaCollection_IsLoadingChanged(object sender, EventArgs e)
        {
            if (MediaState.UIMediaCollection.NrLoadedItems == MediaState.UIMediaCollection.Count())
            {
                SelectAllCommand.IsExecutable = true;
            }
            else
            {
                SelectAllCommand.IsExecutable = false;
            }
        }

        void setupViews()
        {
                        
            // search
            Uri searchViewUri = new Uri(typeof(YoutubeSearchView).FullName, UriKind.Relative);

            RegionManager.RequestNavigate("youtubeSearchExpander", searchViewUri);

            // browse
            Uri browseViewUri = new Uri(typeof(YoutubeChannelBrowserView).FullName, UriKind.Relative);

            RegionManager.RequestNavigate("youtubeSearchExpander", browseViewUri);

            // tagfilter

            Uri tagFilterViewUri = new Uri(typeof(TagFilterView).FullName, UriKind.Relative);

            NavigationParameters tagFilterParams = new NavigationParameters();

            tagFilterParams.Add("MediaStateCollectionView", MediaStateCollectionView);

            RegionManager.RequestNavigate("youtubeFilterExpander", tagFilterViewUri, tagFilterParams);

            // metadata

            Uri youtubeMetadataViewUri = new Uri(typeof(YoutubeMetadataView).FullName, UriKind.Relative);

            RegionManager.RequestNavigate("youtubeMetadataExpander", youtubeMetadataViewUri);
           
            // preview
            Uri mediaPreviewUri = new Uri(typeof(MediaPreviewView).FullName, UriKind.Relative);
            NavigationParameters navigationParams = new NavigationParameters();
            navigationParams.Add("MediaPreviewViewModel", new YoutubePreviewViewModel(EventAggregator));

            RegionManager.RequestNavigate("youtubePreviewExpander", mediaPreviewUri, navigationParams);
        }

        void mediaStateCollectionView_SelectionChanged(object sender, EventArgs e)
        {
            List<MediaItem> mediaItems = MediaStateCollectionView.getSelectedItems();
            List<YoutubeItem> youtubeItems = new List<YoutubeItem>();

            foreach(MediaItem item in mediaItems) {
                youtubeItems.Add(item as YoutubeItem);
            }

            EventAggregator.GetEvent<SelectionEvent>().Publish(youtubeItems);
        }

        int nrColumns;

        public int NrColumns
        {
            get { return nrColumns; }
            set { SetProperty(ref nrColumns, value); }
        }

        public Command<SelectableMediaItem> ViewCommand { get; protected set; }
        public AsyncCommand LoadNextPageCommand { get; protected set; }
        public AsyncCommand<SelectableMediaItem> ViewChannelCommand { get; protected set; }
        public AsyncCommand<SelectableMediaItem> ViewPlaylistCommand { get; protected set; }
        public AsyncCommand<SelectableMediaItem> DownloadCommand { get; protected set; }
        public Command<SelectableMediaItem> SubscribeCommand { get; protected set; }
        public Command SelectAllCommand { get; protected set; }
        public Command DeselectAllCommand { get; protected set; }
        public Command ShutdownCommand { get; protected set; }

        public MediaState MediaState { get; protected set; }
        public YoutubeCollectionView MediaStateCollectionView { get; protected set; }        

        String NextPageToken { get; set; }
        public IClientServiceRequest CurrentQuery { get; set; }

        private async Task searchAsync(IClientServiceRequest request, String searchInfo, bool isNextPage)
        {
            if (SearchTask != null && !SearchTask.IsCompleted)
            {
                try
                {
                    TokenSource.Cancel();
                    await SearchTask;
                }
                catch (OperationCanceledException)
                {

                }
                finally
                {
                    TokenSource = new CancellationTokenSource();
                }
            }

            try
            {
                SearchTask = search(request, searchInfo, isNextPage, TokenSource.Token);
                await SearchTask;
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Youtube Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task search(IClientServiceRequest request, String searchInfo, bool isNextPage, CancellationToken token)
        {
            SearchListResponse searchResponse = null;
            PlaylistItemListResponse playlistItemResponse = null;
            PlaylistListResponse playlistResponse = null;

            SearchResource.ListRequest searchRequest = request as SearchResource.ListRequest;
            PlaylistItemsResource.ListRequest playlistItemsRequest = request as PlaylistItemsResource.ListRequest;
            PlaylistsResource.ListRequest playlistRequest = request as PlaylistsResource.ListRequest;

            List<YoutubeItem> items = new List<YoutubeItem>();  
            int relevance;

            if (isNextPage)
            {
                if (NextPageToken == null)
                {
                    // Final page
                    return;
                }

                if (searchRequest != null)
                {
                    searchRequest.PageToken = NextPageToken;
                }
                else if (playlistItemsRequest != null) 
                {
                    playlistItemsRequest.PageToken = NextPageToken;
                }
                else if (playlistRequest != null)
                {
                    playlistRequest.PageToken = NextPageToken;
                }

                relevance = MediaState.UIMediaCollection.Count;
            }
            else
            {
                MediaState.clearUIState(searchInfo, DateTime.Now, MediaStateType.SearchResult);

                CurrentQuery = request;
                relevance = 0;
            }                       
          
            // Call the search.list method to retrieve results matching the specified query term.
            if (searchRequest != null)
            {
                searchResponse = await searchRequest.ExecuteAsync(token);

                NextPageToken = searchResponse.NextPageToken;
                
                foreach (SearchResult searchResult in searchResponse.Items)
                {
                    YoutubeItem newItem = null;

                    switch (searchResult.Id.Kind)
                    {
                        case "youtube#video":
                            newItem = new YoutubeVideoItem(searchResult, relevance);
                            break;

                        case "youtube#channel":
                            newItem = new YoutubeChannelItem(searchResult, relevance);
                            break;

                        case "youtube#playlist":
                            newItem = new YoutubePlaylistItem(searchResult, relevance);
                            break;
                        default:
                            break;
                    }

                    if (newItem == null || MediaState.UIMediaCollection.Contains(newItem)) continue;

                    items.Add(newItem);

                    relevance++;
                }
               
            }

            if (playlistItemsRequest != null)
            {
                playlistItemResponse = await playlistItemsRequest.ExecuteAsync(token);
                NextPageToken = playlistItemResponse.NextPageToken;

                foreach (PlaylistItem playlistItem in playlistItemResponse.Items)
                {
                    YoutubeVideoItem newItem = new YoutubeVideoItem(playlistItem, relevance);

                    items.Add(newItem);

                    relevance++;
                }
            }

            if (playlistRequest != null)
            {
                playlistResponse = await playlistRequest.ExecuteAsync(token);
                NextPageToken = playlistResponse.NextPageToken;

                foreach (Playlist playlist in playlistResponse.Items)
                {
                    YoutubePlaylistItem newItem = new YoutubePlaylistItem(playlist, relevance);

                    if (!items.Contains(newItem))
                    {
                        items.Add(newItem);
                    }

                    relevance++;
                }

            }
                                              
            // Add each result to the appropriate list, and then display the lists of
            // matching videos, channels, and playlists.                        
            MediaState.addUIState(items);
        }

        public async void searchEvent(YoutubeSearchQuery query)
        {
            await searchAsync(query.Request, query.QueryName, false);
        }
    }
}
