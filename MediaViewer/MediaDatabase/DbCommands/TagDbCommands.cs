﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaViewer.MediaDatabase.DbCommands
{
    class TagDbCommands : DbCommands<Tag>
    {

        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public TagDbCommands(MediaDatabaseContext existingContext = null) :
            base(existingContext)
        {

        }
         
        public List<Tag> getAllTags()
        {
            List<Tag> tags = new List<Tag>();

            foreach (Tag tag in Db.TagSet.OrderBy(x => x.Name))
            {
                tags.Add(tag);
            }

            return (tags);
        }

        public Tag getTagByName(String name)
        {
            List<Tag> result = (from b in Db.TagSet.Include("ChildTags").Include("TagCategory")
                                where b.Name == name
                                select b).ToList();

            if (result.Count == 0) return (null);
            else return (result[0]);
        }

        public Tag getTagById(int id)
        {
            List<Tag> result = (from b in Db.TagSet.Include("ChildTags").Include("TagCategory")
                                where b.Id == id
                                select b).ToList();

            if (result.Count == 0) return (null);
            else return (result[0]);
        }
     
        protected override Tag createFunc(Tag tag)
        {
            if (String.IsNullOrEmpty(tag.Name) || String.IsNullOrWhiteSpace(tag.Name))
            {
                throw new DbEntityValidationException("Error creating tag, name cannot be null, empty or whitespace");
            }

            if (Db.TagSet.Any(t => t.Name == tag.Name))
            {
                throw new DbEntityValidationException("Cannot create duplicate tag: " + tag.Name);
            }

            Tag newTag = new Tag();
            Db.TagSet.Add(newTag);

            Db.Entry<Tag>(newTag).CurrentValues.SetValues(tag);
            newTag.Id = 0;

            TagCategoryDbCommands tagCategoryCommands = new TagCategoryDbCommands(Db);

            // Attach tagcategory to the context, otherwise a duplicate tagcategory will be created
            if (tag.TagCategory != null)
            {
                newTag.TagCategory = tagCategoryCommands.getCategoryById(tag.TagCategory.Id);
            }

            newTag.ChildTags.Clear();

            foreach (Tag childTag in tag.ChildTags)
            {
                Tag child = getTagById(childTag.Id);

                if (child == null)
                {
                    log.Warn("Cannot add non-existent child tag: " + childTag.Id.ToString() + " to parent: " + tag.Id.ToString());
                    continue;
                }

                newTag.ChildTags.Add(child);
            }
         
            Db.SaveChanges();

            return (newTag);
        }

        protected override void deleteFunc(Tag tag)
        {

            Tag deleteTag = getTagById(tag.Id);

            if (deleteTag == null)
            {
                throw new DbEntityValidationException("Cannot delete non-existing tag: " + tag.Id.ToString());
            }


            for (int i = deleteTag.ParentTags.Count - 1; i >= 0; i--)
            {
                Db.Entry<Tag>(deleteTag.ParentTags.ElementAt(i)).State = EntityState.Modified;
                deleteTag.ParentTags.ElementAt(i).ChildTags.Remove(tag);

            }

            Db.TagSet.Remove(deleteTag);
            Db.SaveChanges();
        }



        protected override Tag updateFunc(Tag updateTag)
        {
            if (String.IsNullOrEmpty(updateTag.Name) || String.IsNullOrWhiteSpace(updateTag.Name))
            {
                throw new DbEntityValidationException("Error updating tag, name cannot be null, empty or whitespace");
            }

            Tag tag = getTagById(updateTag.Id);

            if (tag == null)
            {
                throw new DbEntityValidationException("Cannot update non-existing tag id: " + updateTag.Id.ToString());
            }

            Db.Entry<Tag>(tag).CurrentValues.SetValues(updateTag);

            TagCategoryDbCommands tagCategoryCommands = new TagCategoryDbCommands(Db);

            if (updateTag.TagCategory != null)
            {
                tag.TagCategory = tagCategoryCommands.getCategoryById(updateTag.TagCategory.Id);
            }
            else
            {
                tag.TagCategory = null;
            }


            tag.ChildTags.Clear();

            foreach (Tag updateChild in updateTag.ChildTags)
            {
                Tag child = getTagById(updateChild.Id);

                if (child == null)
                {
                    log.Warn("Cannot add non-existent child tag: " + updateChild.Id.ToString() + " to parent: " + tag.Id.ToString());
                    continue;
                }

                tag.ChildTags.Add(child);
            }
     

            Db.SaveChanges();

            return (tag);
        }

    }
}
