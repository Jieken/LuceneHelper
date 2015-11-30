using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;

namespace LuceneHelp
{
    public class QueryInfo<T>
    {
        public int ReturnCount { get; set; }
        public string ReturnFields { get; set; }
        public Query Querys { get; set; }
        public Sort Sorts { get; set; }
        public Filter Filters { get; set; }

        public int Total { get; set; }
        public List<T> Data { get; set; }
    }
}
