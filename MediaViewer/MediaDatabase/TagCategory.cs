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
    
    public partial class TagCategory
    {
        public TagCategory()
        {
            this.Tag = new HashSet<Tag>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] TimeStamp { get; set; }
    
        public virtual ICollection<Tag> Tag { get; set; }
    }
}
