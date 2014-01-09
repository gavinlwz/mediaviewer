﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.MediaDatabase.DbCommands
{
    class PresetMetadataDbCommands : DbCommands
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public PresetMetadataDbCommands(MediaDatabaseContext existingContext = null) : base(existingContext) {

        }

        public List<PresetMetadata> getAllPresets()
        {
            List<PresetMetadata> presets = new List<PresetMetadata>();

            foreach (PresetMetadata preset in Db.PresetMetadataSet.OrderBy(x => x.Name))
            {                
                presets.Add(preset);
            }

            return (presets);
        }

        public PresetMetadata getPresetMetadataById(int id) {

            PresetMetadata result = Db.PresetMetadataSet.Include("Tags").FirstOrDefault(x => x.Id == id);

            return (result);
        }

        public PresetMetadata createPresetMetadata(PresetMetadata preset)
        {
            if (String.IsNullOrEmpty(preset.Name) || String.IsNullOrWhiteSpace(preset.Name))
            {
                throw new DbEntityValidationException("Error updating presetMetadata, name cannot be null, empty or whitespace");
            }

            PresetMetadata newPreset = new PresetMetadata();

            Db.PresetMetadataSet.Add(newPreset);

            newPreset.Name = preset.Name;           
            newPreset.Author = preset.Author;
            newPreset.IsAuthorEnabled = preset.IsAuthorEnabled;
            newPreset.Copyright = preset.Copyright;
            newPreset.IsCopyrightEnabled = preset.IsCopyrightEnabled;
            newPreset.Description = preset.Description;
            newPreset.IsDescriptionEnabled = preset.IsDescriptionEnabled;
            newPreset.Rating = preset.Rating;
            newPreset.IsRatingEnabled = preset.IsRatingEnabled;
            newPreset.Title = preset.Title;
            newPreset.IsTitleEnabled = preset.IsTitleEnabled;
            newPreset.CreationDate = preset.CreationDate;
            newPreset.IsCreationDateEnabled = preset.IsCreationDateEnabled;

            TagDbCommands tagCommands = new TagDbCommands(Db);

            foreach (Tag tag in preset.Tags)
            {
                Tag addTag = tagCommands.getTagById(tag.Id);

                if (addTag != null)
                {
                    newPreset.Tags.Add(addTag);
                }

            }

            Db.SaveChanges();

            return (newPreset);
        }

        public PresetMetadata updatePresetMetadata(PresetMetadata updatePreset)
        {
            if (String.IsNullOrEmpty(updatePreset.Name) || String.IsNullOrWhiteSpace(updatePreset.Name))
            {
                throw new DbEntityValidationException("Error updating presetMetadata, name cannot be null, empty or whitespace");
            }

            PresetMetadata preset = getPresetMetadataById(updatePreset.Id);

            if (preset == null)
            {
                throw new DbEntityValidationException("Cannot update non-existing tag id: " + updatePreset.Id.ToString());
            }

            preset.Name = updatePreset.Name;
            preset.Author = updatePreset.Author;
            preset.IsAuthorEnabled = updatePreset.IsAuthorEnabled;
            preset.Copyright = updatePreset.Copyright;
            preset.IsCopyrightEnabled = updatePreset.IsCopyrightEnabled;
            preset.Description = updatePreset.Description;
            preset.IsDescriptionEnabled = updatePreset.IsDescriptionEnabled;
            preset.Rating = updatePreset.Rating;
            preset.IsRatingEnabled = updatePreset.IsRatingEnabled;
            preset.Title = updatePreset.Title;
            preset.IsTitleEnabled = updatePreset.IsTitleEnabled;
            preset.CreationDate = updatePreset.CreationDate;
            preset.IsCreationDateEnabled = updatePreset.IsCreationDateEnabled;

            preset.Tags.Clear();

            TagDbCommands tagCommands = new TagDbCommands(Db);

            foreach (Tag updateTag in updatePreset.Tags)
            {
                Tag tag = tagCommands.getTagById(updateTag.Id);

                if (tag == null)
                {
                    log.Warn("Cannot add non-existent tag: " + updateTag.Id.ToString() + " to presetMetadata: " + preset.Id.ToString());
                    continue;
                }

                preset.Tags.Add(tag);
            }

            Db.SaveChanges();

            return (preset);
        }

        public void deletePresetMetadata(PresetMetadata preset)
        {
            PresetMetadata deletePreset = getPresetMetadataById(preset.Id);

            if (deletePreset == null)
            {
                throw new DbEntityValidationException("Cannot delete non-existing presetMetadata: " + preset.Id.ToString());
            }
         
            Db.PresetMetadataSet.Remove(deletePreset);
            Db.SaveChanges();
        }
    }
}
