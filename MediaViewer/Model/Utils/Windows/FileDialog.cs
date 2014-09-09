﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace MediaViewer.Model.Utils.Windows
{
    class FileDialog
    {
        public static OpenFileDialog createOpenMediaFileDialog(bool imageOnly)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            String imageFiles = "Image Files|";
            String videoFiles = "Video Files|";
            String allFiles = "All Files|*.*";

            foreach (KeyValuePair<String, String> pair in MediaFormatConvert.extToMimeType)
            {

                String filter = "*." + pair.Key + ";";

                if (pair.Value.StartsWith("image"))
                {

                    imageFiles += filter;

                }
                else
                {

                    videoFiles += filter;
                }
            }

            imageFiles = imageFiles.Remove(imageFiles.Length - 1) + "|";
            videoFiles = videoFiles.Remove(videoFiles.Length - 1) + "|";

            if (imageOnly == true)
            {
                videoFiles = "";
            }

            openFileDialog.Filter = imageFiles + videoFiles + allFiles;
            openFileDialog.FilterIndex = 1;

            return (openFileDialog);
        }

        public static SaveFileDialog createSaveTagsFileDialog()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.FileName = "tags.xml";
            saveFileDialog.Filter = "XML Files|*.xml";
            saveFileDialog.FilterIndex = 1;

            return (saveFileDialog);
        }

        public static OpenFileDialog createLoadTagsFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
         
            openFileDialog.Filter = "XML Files|*.xml";
            openFileDialog.FilterIndex = 1;

            return (openFileDialog);
        }
    }
}
