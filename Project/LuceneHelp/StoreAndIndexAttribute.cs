using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;

namespace LuceneHelp
{
    [AttributeUsage(AttributeTargets.Property)]
    public class StoreAndIndexAttribute : Attribute
    {
        [DefaultValue(Field.Store.YES)]
        public Field.Store store { get; set; }

        [DefaultValue(Field.Index.NOT_ANALYZED)]
        public Field.Index index { get; set; }

        [DefaultValue(1f)]
        public float boost { get; set; }
    }
}
