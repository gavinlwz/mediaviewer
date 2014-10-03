﻿using MediaViewer.Model.Media.File;
using MediaViewer.Model.Media.State.CollectionView;
using Microsoft.Practices.Prism.PubSubEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.Model.GlobalEvents
{
   
    public class MediaBrowserDisplayEvent : PubSubEvent<MediaBrowserDisplayOptions> { }

    public class MediaBrowserDisplayOptions
    {
        public MediaBrowserDisplayOptions()
        {
            IsHidden = false;
            FilterMode = null;
            SelectedItem = null;
            IsEnabled = true;
        }

        public Nullable<bool> IsEnabled { get; set; }

        public Nullable<bool> IsHidden { get; set; }
             
        public Nullable<MediaStateFilterMode> FilterMode { get; set; }

        public MediaFileItem SelectedItem { get; set; }

    }
}
