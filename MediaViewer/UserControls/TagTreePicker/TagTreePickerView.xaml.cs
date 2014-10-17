﻿using Aga.Controls.Tree;
using MediaViewer.MediaDatabase;
using MediaViewer.Model.Collections;
using MediaViewer.Model.Collections.Sort;
using MediaViewer.Model.Utils;
using MediaViewer.TagEditor;
using Microsoft.Practices.Prism.PubSubEvents;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MediaViewer.UserControls.TagTreePicker
{

    public partial class TagTreePickerView : UserControl
    {
        IEventAggregator EventAggregator { get; set; }
    
        public TagTreePickerView()
        {
            InitializeComponent();
            EventAggregator = ServiceLocator.Current.GetInstance(typeof(IEventAggregator)) as IEventAggregator;

            treeView.Root = new CategoryItem(null);

            EventAggregator.GetEvent<TagCategoryCreatedEvent>().Subscribe(addCategory);
            EventAggregator.GetEvent<TagCategoryDeletedEvent>().Subscribe(removeCategory);
            EventAggregator.GetEvent<TagCategoryUpdatedEvent>().Subscribe(updateCategory);

            EventAggregator.GetEvent<TagCreatedEvent>().Subscribe(addTag);
            EventAggregator.GetEvent<TagDeletedEvent>().Subscribe(removeTag);
            EventAggregator.GetEvent<TagUpdatedEvent>().Subscribe(updateTag);
            
        }
       
        public void unregisterMessages() {

            EventAggregator.GetEvent<TagCategoryCreatedEvent>().Unsubscribe(addCategory);
            EventAggregator.GetEvent<TagCategoryDeletedEvent>().Unsubscribe(removeCategory);
            EventAggregator.GetEvent<TagCategoryUpdatedEvent>().Unsubscribe(updateCategory);

            EventAggregator.GetEvent<TagCreatedEvent>().Unsubscribe(addTag);
            EventAggregator.GetEvent<TagDeletedEvent>().Unsubscribe(removeTag);
            EventAggregator.GetEvent<TagUpdatedEvent>().Unsubscribe(updateTag);
        }

        public ObservableRangeCollection<Tag> SelectedTags
        {
            get { return (ObservableRangeCollection<Tag>)GetValue(SelectedTagsProperty); }
            set { SetValue(SelectedTagsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedTag.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedTagsProperty =
            DependencyProperty.Register("SelectedTags", typeof(ObservableRangeCollection<Tag>), typeof(TagTreePickerView), new PropertyMetadata(new ObservableRangeCollection<Tag>()));


        public ObservableRangeCollection<TagCategory> SelectedCategories
        {
            get { return (ObservableRangeCollection<TagCategory>)GetValue(SelectedCategoriesProperty); }
            set { SetValue(SelectedCategoriesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedCategory.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedCategoriesProperty =
            DependencyProperty.Register("SelectedCategories", typeof(ObservableRangeCollection<TagCategory>), typeof(TagTreePickerView), new PropertyMetadata(new ObservableRangeCollection<TagCategory>()));
                
        
        private void treeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {           
            if (treeView.SelectedItems.Count == 0)
            {
                SelectedTags.Clear(); 
                SelectedCategories.Clear(); 
                return;
            }

            List<Tag> selectedTags = new List<Tag>();
            List<TagCategory> selectedCategories = new List<TagCategory>();

            foreach (TagTreePickerItem item in treeView.SelectedItems)
            {               
                if (item is TagItem)
                {
                    selectedTags.Add((item as TagItem).Tag);
                }
                else if (item is CategoryItem)
                {
                    selectedCategories.Add((item as CategoryItem).Category);
                }
            }

            SelectedTags.ReplaceRange(selectedTags);
            SelectedCategories.ReplaceRange(selectedCategories);
        }

        int getNrCategories()
        {
            int result = 0;
            TagTreePickerItem root = treeView.Root as TagTreePickerItem;

            foreach (TagTreePickerItem item in root.Children)
            {
                if (item is CategoryItem)
                {
                    result++;
                }
            }

            return (result);
        }

        private void updateTag(Tag tag)
        {
            removeTag(tag);
            addTag(tag);
        }

        private void removeTag(Tag tag)
        {
            TagTreePickerItem root = treeView.Root as TagTreePickerItem;
            TagItem item = root.findTag(tag.Id);
            if (item != null)
            {
                TagTreePickerItem parent = item.Parent as TagTreePickerItem;
                parent.DeleteWithoutConfirmation(new TagTreePickerItem[] { item });
            }
        }

        private void addTag(Tag tag)
        {
            if (tag.TagCategory == null)
            {
                CollectionsSort.insertIntoSortedCollection(treeView.Root.Children, new TagItem(tag),
                    compareTreeNodes, getNrCategories(), treeView.Root.Children.Count); 
             
            }
            else
            {
                TagTreePickerItem root = treeView.Root as TagTreePickerItem;
                CategoryItem item = root.findCategory(tag.TagCategory.Id);

                if (item != null && item.IsLoaded)
                {
                    CollectionsSort.insertIntoSortedCollection(item.Children, new TagItem(tag), compareTreeNodes);                   
                }
            }
        }

        private void updateCategory(TagCategory category)
        {
            removeCategory(category);
            addCategory(category);
        }

        private void removeCategory(TagCategory category)
        {
            TagTreePickerItem root = treeView.Root as TagTreePickerItem;
            CategoryItem item = root.findCategory(category.Id);

            if (item != null)
            {
                TagTreePickerItem parent = item.Parent as TagTreePickerItem;
                parent.DeleteWithoutConfirmation(new TagTreePickerItem[] { item });                
            }
        }

        private void addCategory(TagCategory category)
        {
            CollectionsSort.insertIntoSortedCollection(treeView.Root.Children, new CategoryItem(category),
                    compareTreeNodes, 0, getNrCategories());             
        }

        private int compareTreeNodes(ICSharpCode.TreeView.SharpTreeNode a, ICSharpCode.TreeView.SharpTreeNode b)
        {
            if (a.GetType() == typeof(TagItem) && b.GetType() == typeof(CategoryItem))
            {
                return (1);
            }

            if (a.GetType() == typeof(CategoryItem) && b.GetType() == typeof(TagItem))
            {
                return (-1);
            }

            return(a.ToString().CompareTo(b.ToString()));
        }

        public void reloadAll()
        {
            treeView.Root = new CategoryItem(null);
        }
    }
}
