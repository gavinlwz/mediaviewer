//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MediaViewer.MediaDatabase
{
    using System;
    using System.Collections.Generic;
    
    public partial class Tag
    {
        public Tag()
        {
            this.Used = 0;
            this.Media = new HashSet<Media>();
            this.ChildTags = new HashSet<Tag>();
            this.ParentTags = new HashSet<Tag>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public Nullable<int> TagCategoryId { get; set; }
        public long Used { get; set; }
        public byte[] TimeStamp { get; set; }
    
        public virtual TagCategory TagCategory { get; set; }
        public virtual ICollection<Media> Media { get; set; }
        public virtual ICollection<Tag> ChildTags { get; set; }
        public virtual ICollection<Tag> ParentTags { get; set; }
    }
}
