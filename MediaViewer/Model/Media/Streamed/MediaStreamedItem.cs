﻿using MediaViewer.MediaDatabase;
using MediaViewer.Model.Media.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.Model.Media.Streamed
{
    public class MediaStreamedItem : MediaItem
    {
        public MediaStreamedItem(String location, String name = null, MediaItemState state = MediaItemState.EMPTY, bool isReadOnly = true) :
            base(location, name, state, isReadOnly)
        {
           
        }

        public override void readMetadata_URLock(metadata.Metadata.MetadataFactory.ReadOptions options, System.Threading.CancellationToken token)
        {           
            ItemState = MediaItemState.LOADED;
        }

        
    }
}
