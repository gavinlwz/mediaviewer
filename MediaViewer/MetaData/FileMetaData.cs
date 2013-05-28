﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DB = MediaDatabase;
using XMPLib;
using MediaViewer.MetaData.MetaDataTree;
using System.Globalization;


namespace MediaViewer.MetaData
{
    class FileMetaData : EventArgs, IDisposable
    {

        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string filePath;
        private string title;
        private string description;
        private string creator;
        private string creatorTool;
        private string copyright;
        private List<MetaDataThumb> thumbnail;
        private List<string> tags;
        private DateTime creationDate;
        private DateTime modifiedDate;
        private DateTime metaDataDate;

        private const string latString = "geo:lat=";
        private const string lonString = "geo:lon=";
        private GeoTagCoordinatePair geoTag;
        private bool hasGeoTag;

        private XMPLib.MetaData metaData;
        private MetaDataTreeNode tree;

        private void deleteThumbNails()
        {

            if (thumbnail != null)
            {
             
                thumbnail.Clear();
            }

        }

        private void readThumbnails()
        {

            thumbnail.Clear();

            int nrThumbs = metaData.countArrayItems(Consts.XMP_NS_XMP, "Thumbnails");

            for (int thumbNr = 1; thumbNr <= nrThumbs; thumbNr++)
            {

                string fullPath = "";

                XMPLib.MetaData.composeArrayItemPath(Consts.XMP_NS_XMP, "Thumbnails", thumbNr, ref fullPath);
                XMPLib.MetaData.composeStructFieldPath(Consts.XMP_NS_XMP, fullPath, Consts.XMP_NS_XMP_Image, "image", ref fullPath);

                string encodedData = "";

                bool success = metaData.getProperty(Consts.XMP_NS_XMP, fullPath, ref encodedData);

                if (!success) continue;

                byte[] decodedData = Convert.FromBase64String(encodedData);

                MemoryStream stream = new MemoryStream();
                stream.Write(decodedData, 0, decodedData.Length);
                stream.Seek(0, SeekOrigin.Begin);

                thumbnail.Add(new MetaDataThumb(stream));
            }
        }

        private void writeThumbnails()
        {

            int nrThumbs = metaData.countArrayItems(Consts.XMP_NS_XMP, "Thumbnails");

            for (int i = nrThumbs; i > 0; i--)
            {

                metaData.deleteArrayItem(Consts.XMP_NS_XMP, "Thumbnails", i);
            }

            for (int i = 0; i < thumbnail.Count; i++)
            {

                byte[] decodedData = thumbnail[i].Data.ToArray();

                string encodedData = Convert.ToBase64String(decodedData);

                string path = "";

                metaData.appendArrayItem(Consts.XMP_NS_XMP, "Thumbnails", 
                    Consts.PropOptions.XMP_PropValueIsArray, null,
                    Consts.PropOptions.XMP_PropValueIsStruct);
                XMPLib.MetaData.composeArrayItemPath(Consts.XMP_NS_XMP, "Thumbnails",
                    Consts.XMP_ArrayLastItem, ref path);

                metaData.setStructField(Consts.XMP_NS_XMP, path, 
                    Consts.XMP_NS_XMP_Image, "image", encodedData, 0);
                metaData.setStructField(Consts.XMP_NS_XMP, path,
                    Consts.XMP_NS_XMP_Image, "format", "JPEG", 0);
                metaData.setStructField(Consts.XMP_NS_XMP, path, 
                    Consts.XMP_NS_XMP_Image, "width", Convert.ToString(thumbnail[i].Width), 0);
                metaData.setStructField(Consts.XMP_NS_XMP, path, 
                    Consts.XMP_NS_XMP_Image, "height", Convert.ToString(thumbnail[i].Height), 0);
            }
        }

        private void initialize(string filePath)
        {

            this.filePath = filePath;
            title = "";
            description = "";
            creator = "";
            creatorTool = "";
            copyright = "";
            thumbnail = new List<MetaDataThumb>();
            tags = new List<string>();
            creationDate = DateTime.MinValue;
            modifiedDate = DateTime.MinValue;
            metaDataDate = DateTime.MinValue;

            geoTag = new GeoTagCoordinatePair();
            hasGeoTag = false;

            tree = null;
            metaData = null;

        }

        private void initVarsFromDatabaseItem(DB.Media item)
        {

            FilePath = item.Location;

            if (item.Title != null)
            {

                Title = item.Title;
            }

            if (item.Description != null)
            {

                Description = item.Description;
            }

            if (item.Author != null)
            {

                Creator = item.Author;
            }

            if (item.Copyright != null)
            {

                Copyright = item.Copyright;
            }

            if (item.CreatorTool != null)
            {

                CreatorTool = item.CreatorTool;
            }

            if (item.MetaDataLastModifiedDate.HasValue)
            {

                ModifiedDate = item.MetaDataLastModifiedDate.Value;
            }

            if (item.MetaDataDate.HasValue)
            {

                MetaDataDate = item.MetaDataDate.Value;
            }

            if (item.MetaDataCreationDate.HasValue)
            {

                CreationDate = item.MetaDataCreationDate.Value;
            }

            if (item.GeoTagLongitude != null && item.GeoTagLatitude != null)
            {

                hasGeoTag = true;
                GeoTag.Longitude.Coord = item.GeoTagLongitude;
                GeoTag.Latitude.Coord = item.GeoTagLatitude;
            }

            foreach (DB.MediaTag mediaTag in item.MediaTag)
            {

                Tags.Add(mediaTag.Tag);
            }

            foreach (DB.MediaThumb mediaThumb in item.MediaThumb)
            {

                MemoryStream stream = new MemoryStream(mediaThumb.ImageData);

                MetaDataThumb thumb = new MetaDataThumb(stream);

                Thumbnail.Add(thumb);
            }
        }



        public FileMetaData()
        {

            initialize("");
        }

        public FileMetaData(DB.Media media)
        {

            initialize("");

            initVarsFromDatabaseItem(media);
        }


        public void load(string filePath)
        {
           
            initialize(filePath);

            DB.Context ctx = null;

            try
            {

                ctx = new DB.Context();

                DB.Media item = ctx.getMediaByLocation(filePath);

                FileInfo file = new FileInfo(filePath);

                if (item == null || DB.Context.isMediaItemOutdated(item, file))
                {

                    ctx.close();

                    loadFromDisk(filePath);
                    saveToDatabase();

                }
                else
                {

                    initVarsFromDatabaseItem(item);

                }

            }
            finally
            {

                if (ctx != null)
                {

                    ctx.close();
                }
            }

        }

        public bool loadFromDataBase(string filePath)
        {

            DB.Context ctx = null;

            try
            {

                initialize(filePath);

                ctx = new DB.Context();

                DB.Media item = ctx.getMediaByLocation(filePath);

                if (item == null) return (false);

                initVarsFromDatabaseItem(item);

                return (true);

            }
            finally
            {

                if (ctx != null)
                {

                    ctx.close();
                }
            }
        }

        public void loadFromDisk(string filePath)
        {

            initialize(filePath);

            metaData = new XMPLib.MetaData();

            if (metaData.open(filePath, Consts.OpenOptions.XMPFiles_OpenForRead) == false)
            {

                throw new Exception("Cannot open metadata for: " + filePath);

            }

            readThumbnails();

            string temp = "";

            bool exists = metaData.getLocalizedText(Consts.XMP_NS_DC, "title", "en", "en-US", ref temp);
            if (exists)
            {

                Title = temp;
            }

            exists = metaData.getLocalizedText(Consts.XMP_NS_DC, "description", "en", "en-US",ref temp);
            if (exists)
            {

                Description = temp;
            }

            exists = metaData.getArrayItem(Consts.XMP_NS_DC, "creator", 1, ref temp);
            if (exists)
            {

                Creator = temp;
            }

            exists = metaData.getArrayItem(Consts.XMP_NS_DC, "rights", 1, ref temp);
            if (exists)
            {

                Copyright = temp;
            }

            exists = metaData.getProperty(Consts.XMP_NS_XMP, "CreatorTool", ref temp);
            if (exists)
            {

                CreatorTool = temp;
            }

            DateTime propValue = DateTime.MinValue;

            exists = metaData.getProperty_Date(Consts.XMP_NS_XMP, "MetadataDate", ref propValue);
            if (exists)
            {

                MetaDataDate = propValue;
            }

            exists = metaData.getProperty_Date(Consts.XMP_NS_XMP, "CreateDate", ref propValue);
            if (exists)
            {

                CreationDate = propValue;
            }

            exists = metaData.getProperty_Date(Consts.XMP_NS_XMP, "ModifyDate", ref propValue);
            if (exists)
            {

                ModifiedDate = propValue;
            }

            if (metaData.doesPropertyExists(Consts.XMP_NS_EXIF, "GPSLatitude") && metaData.doesPropertyExists(Consts.XMP_NS_EXIF, "GPSLongitude"))
            {
                string latitude = "";
                string longitude = "";

                metaData.getProperty(Consts.XMP_NS_EXIF, "GPSLatitude", ref latitude);
                metaData.getProperty(Consts.XMP_NS_EXIF, "GPSLongitude", ref longitude);

                geoTag.Longitude.Coord = longitude;
                geoTag.Latitude.Coord = latitude;

                hasGeoTag = true;

            }

            bool hasLat = false;
            bool hasLon = false;

            tags = new List<string>();

            int nrTags = metaData.countArrayItems(Consts.XMP_NS_DC, "subject");

            for (int i = 1; i <= nrTags; i++)
            {

                string tag = "";
                exists = metaData.getArrayItem(Consts.XMP_NS_DC, "subject", i, ref tag);

                if (exists)
                {

                    tags.Add(tag);

                    if (tag.StartsWith(latString))
                    {

                        geoTag.Latitude.Decimal = Convert.ToDouble(tag.Substring(latString.Length), CultureInfo.InvariantCulture);
                        hasLat = true;

                    }
                    else if (tag.StartsWith(lonString))
                    {

                        geoTag.Longitude.Decimal = Convert.ToDouble(tag.Substring(lonString.Length), CultureInfo.InvariantCulture);
                        hasLon = true;
                    }
                }
            }

            if (hasLat && hasLon)
            {

                hasGeoTag = true;
            }

        }

        public void Dispose()
        {
            closeFile();
        }

        public string FilePath
        {

            get
            {

                return (filePath);
            }

            set
            {

                this.filePath = value;
            }
        }

        public GeoTagCoordinatePair GeoTag
        {

            get
            {

                return (geoTag);
            }

            set
            {

                this.geoTag = value;
            }
        }

        public bool HasGeoTag
        {

            get
            {

                return (hasGeoTag);
            }

            set
            {

                this.hasGeoTag = value;
            }
        }

        public string Title
        {

            get
            {

                return (title);
            }

            set
            {

                this.title = value;
            }
        }
        public string Description
        {

            get
            {

                return (description);
            }

            set
            {

                this.description = value;
            }
        }

        public string Creator
        {

            get
            {

                return (creator);
            }

            set
            {

                this.creator = value;
            }
        }
        public string CreatorTool
        {

            get
            {

                return (creatorTool);
            }

            set
            {

                this.creatorTool = value;
            }
        }
        public string Copyright
        {

            get
            {

                return (copyright);
            }

            set
            {

                this.copyright = value;
            }
        }
        public List<string> Tags
        {

            get
            {

                return (tags);
            }

            set
            {

                this.tags = value;
            }
        }
        public DateTime CreationDate
        {

            get
            {

                return (creationDate);
            }

            set
            {

                this.creationDate = value;
            }
        }
        public DateTime ModifiedDate
        {

            get
            {

                return (modifiedDate);
            }

            set
            {

                this.modifiedDate = value;
            }
        }
        public DateTime MetaDataDate
        {

            get
            {

                return (metaDataDate);
            }

            set
            {

                this.metaDataDate = value;
            }
        }

        public MetaDataTreeNode Tree
        {

            get
            {

                if (tree == null && metaData != null)
                {

                    tree = MetaDataTree.MetaDataTree.create(metaData);
                }

                return (tree);
            }
        }

        public List<MetaDataThumb> Thumbnail
        {

            set
            {

                this.thumbnail = value;
            }

            get
            {

                return (thumbnail);
            }
        }

        public void save()
        {

            saveToDisk();
            saveToDatabase();
        }

        public void saveToDatabase()
        {

            try
            {

                DB.Context ctx = new DB.Context();

                DB.Media item = ctx.getMediaByLocation(FilePath);

                FileInfo file = new FileInfo(FilePath);

                bool exists = true;
                if (item == null)
                {

                    item = DB.Context.newMediaItem(file);
                    exists = false;
                }

                item.FileLastWriteTimeTicks = file.LastWriteTime.Ticks;
                item.FileCreationTimeTicks = file.CreationTime.Ticks;

                if (!string.IsNullOrEmpty(Title))
                {

                    item.Title = Title;

                }
                else
                {

                    item.Title = null;
                }

                if (!string.IsNullOrEmpty(Description))
                {

                    item.Description = Description;

                }
                else
                {

                    item.Description = null;
                }

                if (!string.IsNullOrEmpty(CreatorTool))
                {

                    item.CreatorTool = CreatorTool;

                }
                else
                {

                    item.CreatorTool = null;
                }

                if (!string.IsNullOrEmpty(Creator))
                {

                    item.Author = Creator;

                }
                else
                {

                    item.Author = null;
                }

                if (!string.IsNullOrEmpty(Copyright))
                {

                    item.Copyright = Creator;

                }
                else
                {

                    item.Copyright = null;
                }

                if (CreationDate != DateTime.MinValue)
                {

                    item.MetaDataCreationDate = CreationDate;

                }
                else
                {

                    item.MetaDataCreationDate = new Nullable<DateTime>();
                }

                if (ModifiedDate != DateTime.MinValue)
                {

                    item.MetaDataLastModifiedDate = ModifiedDate;

                }
                else
                {

                    item.MetaDataLastModifiedDate = new Nullable<DateTime>();
                }

                if (HasGeoTag == true)
                {

                    item.GeoTagLongitude = GeoTag.Longitude.Coord;
                    item.GeoTagLatitude = GeoTag.Latitude.Coord;
                }

                item.MetaDataDate = DateTime.Now;

                item.MediaTag.Clear();

                List<string> temp = new List<string>();

                foreach (string tag in Tags)
                {

                    if (temp.Contains(tag)) continue;
                    temp.Add(tag);

                    DB.MediaTag mediaTag = new DB.MediaTag();
                    mediaTag.Tag = tag;

                    item.MediaTag.Add(mediaTag);

                }

                int i = 0;

                item.MediaThumb.Clear();

                foreach (MetaDataThumb thumb in Thumbnail)
                {

                    DB.MediaThumb mediaThumb = new DB.MediaThumb();
                    mediaThumb.ImageData = thumb.Data.ToArray();
                    mediaThumb.ThumbNr = i;

                    item.MediaThumb.Add(mediaThumb);
                    i++;
                }

                ////log.Info("saving: " + FilePath + " " + i.ToString() + " thumbs");
                if (exists == false)
                {
                    ctx.insert(item);
                }
                ctx.saveChanges();
                ctx.close();

            }
            catch (Exception e)
            {

                log.Error("DATABASE Failed to store: " + FilePath + " - " + e.Message);

            }
        }

        public virtual void saveToDisk()
        {

            metaData = new XMPLib.MetaData();

            if (metaData.open(filePath, Consts.OpenOptions.XMPFiles_OpenForUpdate) == false)
            {

                throw new Exception("Cannot open metadata for: " + filePath);

            }

            writeThumbnails();

            if (!string.IsNullOrEmpty(Title))
            {

                metaData.setLocalizedText(Consts.XMP_NS_DC, "title", "en", "en-US", Title);

            }

            if (!string.IsNullOrEmpty(Description))
            {

                if (metaData.doesArrayItemExist(Consts.XMP_NS_DC, "description", 1))
                {

                    metaData.setArrayItem(Consts.XMP_NS_DC, "description", 1, Description, 0);

                }
                else
                {

                    metaData.appendArrayItem(Consts.XMP_NS_DC, "description",
                        Consts.PropOptions.XMP_PropArrayIsOrdered, Description, 0);
                }

            }

            if (!string.IsNullOrEmpty(CreatorTool))
            {

                metaData.setProperty(Consts.XMP_NS_XMP, "CreatorTool", CreatorTool, 
                    Consts.PropOptions.XMP_DeleteExisting);
            }

            if (!string.IsNullOrEmpty(Creator))
            {

                if (metaData.doesArrayItemExist(Consts.XMP_NS_DC, "creator", 1))
                {

                    metaData.setArrayItem(Consts.XMP_NS_DC, "creator", 1, Creator, 0);

                }
                else
                {

                    metaData.appendArrayItem(Consts.XMP_NS_DC, "creator", 
                        Consts.PropOptions.XMP_PropArrayIsOrdered, Creator, 0);
                }

            }

            if (!string.IsNullOrEmpty(Copyright))
            {

                if (metaData.doesArrayItemExist(Consts.XMP_NS_DC, "rights", 1))
                {

                    metaData.setArrayItem(Consts.XMP_NS_DC, "rights", 1, Copyright, 0);

                }
                else
                {

                    metaData.appendArrayItem(Consts.XMP_NS_DC, "rights",
                        Consts.PropOptions.XMP_PropArrayIsOrdered, Copyright, 0);
                }

            }

            metaData.setProperty_Date(Consts.XMP_NS_XMP, "MetadataDate", DateTime.Now);

            List<string> tags = Tags;
            int nrTags = metaData.countArrayItems(Consts.XMP_NS_DC, "subject");

            for (int i = nrTags; i > 0; i--)
            {

                metaData.deleteArrayItem(Consts.XMP_NS_DC, "subject", i);
            }

            foreach (string tag in tags)
            {

                metaData.appendArrayItem(Consts.XMP_NS_DC, "subject", 
                    Consts.PropOptions.XMP_PropArrayIsUnordered, tag, 0);
            }

            if (HasGeoTag == true)
            {
                string latitude = geoTag.Latitude.Coord;
                string longitude = geoTag.Longitude.Coord;

                metaData.setProperty(Consts.XMP_NS_EXIF, "GPSLatitude", latitude, 0);
                metaData.setProperty(Consts.XMP_NS_EXIF, "GPSLongitude", longitude, 0);

            }
            else
            {

                //// remove a potentially existing geotag
                if (metaData.doesPropertyExists(Consts.XMP_NS_EXIF, "GPSLatitude") && metaData.doesPropertyExists(Consts.XMP_NS_EXIF, "GPSLongitude"))
                {

                    metaData.deleteProperty(Consts.XMP_NS_EXIF, "GPSLatitude");
                    metaData.deleteProperty(Consts.XMP_NS_EXIF, "GPSLongitude");
                }

                string latString = "geo:lat=";
                string lonString = "geo:lon=";

                for (int i = metaData.countArrayItems(Consts.XMP_NS_DC, "subject"); i > 0; i--)
                {

                    string value = "";

                    metaData.getArrayItem(Consts.XMP_NS_DC, "subject", i, ref value);

                    if (value.StartsWith(latString) || value.StartsWith(lonString))
                    {

                        metaData.deleteArrayItem(Consts.XMP_NS_DC, "subject", i);
                    }
                }
            }

            if (metaData.canPutXMP())
            {

                metaData.putXMP();

            }
            else
            {

                closeFile();
                throw new Exception("Cannot write metadata for: " + filePath);
            }

            closeFile();
        }

        public void closeFile()
        {

            if (metaData != null)
            {

                metaData.Dispose();
                metaData = null;
            }
        }
    }
}
