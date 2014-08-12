﻿using MediaViewer.MediaFileModel.Watcher;
using MediaViewer.MetaData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginTest
{
    public class GeoTagFileData
    {
        String fileUrl;

        public String FileUrl
        {
            get { return fileUrl; }
            set { fileUrl = value; }
        }
        String fileName;

        public String FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        bool isModified;

        public bool IsModified
        {
            get { return isModified; }
            set { isModified = value; }
        }
        int placeMarkIndex;

        public int PlaceMarkIndex
        {
            get { return placeMarkIndex; }
            set { placeMarkIndex = value; }
        }

        bool hasGeoTag;

        public bool HasGeoTag
        {
            get { return hasGeoTag; }
            set { hasGeoTag = value; }
        }

        public GeoTagFileData(MediaFileItem item)
        {
            this.item = item;
            fileName = System.IO.Path.GetFileName(item.Location);
            fileUrl = "file:///" + item.Location.Replace('\\', '/');

		    placeMarkIndex = -1;
		    isModified = false;

            geoTag = new GeoTagCoordinatePair();

            if (item.Media.Latitude != null && item.Media.Longitude != null)
            {
                geoTag.Latitude.Coord = item.Media.Latitude;
                geoTag.Longitude.Coord = item.Media.Longitude;
                HasGeoTag = true;
            }
            else
            {
                HasGeoTag = false;
            }
           
        }

        MediaFileItem item;

        public MediaFileItem Item
        {
            get { return item; }
           
        }

        GeoTagCoordinatePair geoTag;

        public GeoTagCoordinatePair GeoTag
        {
            get { return geoTag; }
            set { geoTag = value; }
        }

    }
}
