using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lucene.Net.Documents;
using LuceneHelp;

namespace TestLucene2
{
    public class People
    {
        [StoreAndIndex(store = Field.Store.YES, index = Field.Index.NOT_ANALYZED, boost = 1f)]
        public string id { get; set; }
        public string Name { get; set; }
        public int Phone { get; set; }
        public decimal Money { get; set; }
    }
}