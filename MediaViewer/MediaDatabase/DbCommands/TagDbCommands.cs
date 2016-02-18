﻿using MediaViewer.Infrastructure.Logging;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaViewer.MediaDatabase.DbCommands
{
    class TagDbCommands : DbCommands<Tag>
    {
        public TagDbCommands(MediaDatabaseContext existingContext = null) :
            base(existingContext)
        {

        }

        public List<Tag> getAllUnusedTags()
        {           
            List<Tag> tags = Db.Tags.Where((t) => t.Used == 0).ToList();

            return (tags);
        }
         
        public List<Tag> getAllTags(bool loadAllReferences = false)
        {
            List<Tag> tags = new List<Tag>();

            if (loadAllReferences == true)
            {
                foreach (Tag tag in Db.Tags.Include(t => t.ChildTags).Include(t => t.ParentTags).OrderBy(x => x.Name))
                {
                    tags.Add(tag);
                }
            }
            else
            {

                foreach (Tag tag in Db.Tags.OrderBy(x => x.Name))
                {
                    tags.Add(tag);
                }
            }

            return (tags);
        }

        public int getNrTags()
        {
            return (Db.Tags.Count());
        }

        public List<Tag> getTagAutocompleteMatches(String query, int maxResults, bool startsWith = false)
        {
            List<Tag> result;

            if (startsWith == false)
            {
                result = Db.Tags.Where(t => t.Name.Contains(query)).OrderByDescending(t => t.Used).Take(maxResults).ToList();
            }
            else
            {
                result = Db.Tags.Where(t => t.Name.StartsWith(query)).OrderByDescending(t => t.Used).Take(maxResults).ToList();
            }

            return (result);
        }

        public Tag getTagByName(String name)
        {
            List<Tag> result = (from b in Db.Tags.Include(t => t.ChildTags)
                                where b.Name == name
                                select b).ToList();

            if (result.Count == 0) return (null);
            else return (result[0]);
        }

        public Tag getTagById(int id)
        {
            List<Tag> result = (from b in Db.Tags.Include(t => t.ChildTags)
                                where b.Id == id
                                select b).ToList();

            if (result.Count == 0) return (null);
            else return (result[0]);
        }

        public int getNrChildTags(Tag tag)
        {
            Tag result = Db.Tags.FirstOrDefault(t => t.Id == tag.Id);

            if (result == null)
            {
                throw new DbEntityValidationException("getNrChildTags error, tag does not exist");
            }
            else
            {
                return (result.ChildTags.Count);
            }
           
        }

        protected override Tag createFunc(Tag tag)
        {
            if (String.IsNullOrEmpty(tag.Name) || String.IsNullOrWhiteSpace(tag.Name))
            {
                throw new DbEntityValidationException("Error creating tag, name cannot be null, empty or whitespace");
            }

            if (Db.Tags.Any(t => t.Name == tag.Name))
            {
                throw new DbEntityValidationException("Cannot create duplicate tag: " + tag.Name);
            }

            Tag newTag = new Tag();
            Db.Tags.Add(newTag);

            Db.Entry<Tag>(newTag).CurrentValues.SetValues(tag);
            newTag.Id = 0;
           
            newTag.ChildTags.Clear();

            foreach (Tag childTag in tag.ChildTags)
            {
                Tag child = getTagById(childTag.Id);

                if (child == null)
                {
                    Logger.Log.Warn("Cannot add non-existent child tag: " + childTag.Id.ToString() + " to parent: " + tag.Id.ToString());
                    continue;
                }

                newTag.ChildTags.Add(child);
            }

            int maxRetries = 15;

            do
            {
                try
                {
                    Db.SaveChanges();
                    maxRetries = 0;
                }
                catch (DbUpdateConcurrencyException e)
                {
                    if (--maxRetries == 0)
                    {
                        throw;
                    }

                    foreach (DbEntityEntry conflictingEntity in e.Entries)
                    {                       
                        // reload the conflicting tag (database wins)
                        conflictingEntity.Reload();                                                   
                    }

                    Random random = new Random();

                    Thread.Sleep(random.Next(50, 100));
                }

            } while (maxRetries > 0);

            return (newTag);
        }

        protected override void deleteFunc(Tag tag)
        {
            Tag deleteTag = getTagById(tag.Id);

            if (deleteTag == null)
            {
                throw new DbEntityValidationException("Cannot delete non-existing tag with id: " + tag.Id.ToString());
            }

            for (int i = deleteTag.ParentTags.Count - 1; i >= 0; i--)
            {
                Db.Entry<Tag>(deleteTag.ParentTags.ElementAt(i)).State = EntityState.Modified;
                deleteTag.ParentTags.ElementAt(i).ChildTags.Remove(tag);
            }

            Db.Tags.Remove(deleteTag);
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
                throw new DbEntityValidationException("Cannot update non-existing tag with id: " + updateTag.Id.ToString());
            }

            Db.Entry<Tag>(tag).CurrentValues.SetValues(updateTag);
           
            tag.ChildTags.Clear();

            foreach (Tag updateChild in updateTag.ChildTags)
            {
                Tag child = getTagById(updateChild.Id);

                if (child == null)
                {
                    Logger.Log.Warn("Cannot add non-existent child tag: " + updateChild.Id.ToString() + " to parent: " + tag.Id.ToString());
                    continue;
                }

                tag.ChildTags.Add(child);
            }
     

            Db.SaveChanges();

            return (tag);
        }

        /// <summary>
        /// If mergetag doesn't exist create it
        /// If mergetag exists add mergetag children to tag and add it's category to mergetag
        /// </summary>
        /// <param name="mergeTag"></param>
        public void merge(Tag mergeTag)
        {             
             Tag existingTag = getTagByName(mergeTag.Name);

             if (existingTag == null)
             {

                 List<Tag> childTags = new List<Tag>();

                 // check if all child tags exist, if not create them
                 foreach (Tag newChildTag in mergeTag.ChildTags)
                 {
                     Tag existingChildTag = getTagByName(newChildTag.Name);

                     if (existingChildTag == null)
                     {
                         newChildTag.ChildTags.Clear();                      
                         existingChildTag = create(newChildTag);
                     }

                     childTags.Add(existingChildTag);
                 }

                 mergeTag.ChildTags.Clear();

                 foreach (Tag tag in childTags)
                 {
                     mergeTag.ChildTags.Add(tag);
                 }

                 create(mergeTag);
             }
             else
             {
                 bool isModified = false;
               
                 foreach (Tag newChildTag in mergeTag.ChildTags)
                 {
                     Tag existingChildTag = getTagByName(newChildTag.Name);

                     if (existingChildTag == null)
                     {
                         newChildTag.ChildTags.Clear();
                      
                         existingTag.ChildTags.Add(create(newChildTag));

                         isModified = true;
                     }
                     else if (!existingTag.ChildTags.Contains(existingChildTag, EqualityComparer<Tag>.Default))
                     {
                         existingTag.ChildTags.Add(existingChildTag);
                         isModified = true;
                     }
                 }

                 if (isModified)
                 {
                     Db.SaveChanges();
                 }
             }

        }

        public override void clearAll()
        {
            String[] tableNames = new String[] {"PresetMetadataTag", "MediaTag", "TagTag", "Tags"};
         
            for (int i = 0; i < tableNames.Count(); i++)
            {
                Db.Database.ExecuteSqlCommand("TRUNCATE TABLE [" + tableNames[i] + "]");
            }

        }
    }
}
