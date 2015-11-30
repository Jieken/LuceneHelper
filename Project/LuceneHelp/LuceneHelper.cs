using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Store;
using blqw;
using Lucene.Net.Documents;
using System.ComponentModel;
using Lucene.Net.Search;

namespace LuceneHelp
{
    public class LuceneHelper<T> where T : new()
    {
        private SemaphoreSlim semaphoreSlim;
        public Analyzer analyzer { get; set; }
        public Directory directory = null;

        public LuceneHelper(string indexName = null)
        {

            this.analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

            if (indexName == null)
            {
                var type = typeof(T);
                indexName = type.Name;
            }
            string indexpath = AppDomain.CurrentDomain.BaseDirectory + @"App_Data\Lucene\" + indexName;
            directory = Lucene.Net.Store.FSDirectory.Open(new System.IO.DirectoryInfo(indexpath), new NoLockFactory());
            semaphoreSlim = LuceneSemaphoreSlim.GetSemaphore(indexName);
        }

        //释放所有资源
        public void Dispose()
        {
            this.directory.Dispose();
            this.analyzer.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 创建IndexWriter
        /// </summary>
        /// <returns></returns>
        public IndexWriter CreateIndexWriter()
        {
            bool isExist = IndexReader.IndexExists(directory); //是否存在索引库文件夹以及索引库特征文件
            if (isExist)
            {
                //如果索引目录被锁定（比如索引过程中程序异常退出或另一进程在操作索引库），则解锁
                //Q:存在问题 如果一个用户正在对索引库写操作 此时是上锁的 而另一个用户过来操作时 将锁解开了 于是产生冲突 --解决方法后续
                if (IndexWriter.IsLocked(directory))
                {
                    IndexWriter.Unlock(directory);
                }
            }
            IndexWriter writer = new IndexWriter(directory, analyzer, !isExist, IndexWriter.MaxFieldLength.LIMITED);
            return writer;
        }

        /// <summary>
        /// 创建IndexReader
        /// </summary>
        /// <returns></returns>
        public IndexReader CreateIndexReader()
        {
            return IndexReader.Open(directory, true);
        }

        /// <summary>
        /// 优化索引
        /// </summary>
        public void OptimizeIndex()
        {
            semaphoreSlim.Wait();
            IndexWriter writer = CreateIndexWriter();
            writer.Optimize();//整理索引片,速度非常慢
            writer.Dispose();
            semaphoreSlim.Release();
        }

        /// <summary>
        /// 根据一个实体创建一个Document
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public Document CreateDoc(T model)
        {
            Document doc = new Document();
            var type = typeof(T);

            var ti = TypesHelper.GetTypeInfo(type); //缓存中获取
            var li = ti.IgnoreCaseLiteracy;


            foreach (var item in li.Property)//遍历所有属性
            {
                string key = item.Name;
                object objval = item.GetValue(model);
                string value = "";


                var classAttribute = item.Attributes.Where<StoreAndIndexAttribute>().FirstOrDefault();

                var store = Field.Store.YES;
                var index = Field.Index.NOT_ANALYZED; //索引不分词
                float boost = 1f;
                if (classAttribute != null)
                {
                    store = classAttribute.store;
                    index = classAttribute.index;
                    boost = classAttribute.boost;
                }

                if (key == "id" && objval == null)
                {
                    value = ObjectId.NewId();
                }

                if (objval != null)
                {
                    value = objval.ToString();
                }

                Field field = new Field(
                                key,
                                value,
                                store,
                                index
                            );
                field.Boost = boost;

                doc.Add(field);

                if (key == "boost")
                {
                    doc.Boost = Convert.ToSingle(objval);
                }
            }

            return doc;
        }

        /// <summary>
        /// 根据一个Document创建一个实体T
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public T CreateModel(Document doc)
        {
            var type = typeof(T);

            T model = new T();

            var fields = doc.GetFields();
            var ti = TypesHelper.GetTypeInfo(type); //缓存中获取
            var li = ti.IgnoreCaseLiteracy;

            foreach (var field in fields)
            {
                string key = field.Name;
                string value = field.StringValue;
                if (value != null)
                {
                    li.Property[key].SetValue(model, value);
                }
            }
            return model;
        }

        public void Insert(T model)//添加一条数据
        {
            semaphoreSlim.Wait();
            IndexWriter writer = CreateIndexWriter();
            writer.AddDocument(CreateDoc(model));
            writer.Commit();
            writer.Dispose();
            semaphoreSlim.Release();
        }

        public void InsertList(IEnumerable<T> list)//批量添加数据
        {
            semaphoreSlim.Wait();
            IndexWriter writer = CreateIndexWriter();
            foreach (var item in list)
            {
                writer.AddDocument(CreateDoc(item));
            }
            writer.Commit();
            writer.Dispose();
            semaphoreSlim.Release();
        }

        public void Update(T model) //更新索引
        {
            semaphoreSlim.Wait();
            Document doc = CreateDoc(model);
            IndexWriter writer = CreateIndexWriter();
            var term = new Term("id", doc.Get("id"));
            writer.UpdateDocument(term, doc);
            writer.Commit();
            writer.Dispose();
            semaphoreSlim.Release();
        }

        public void UpdateList(IEnumerable<T> list) //批量更新索引
        {
            semaphoreSlim.Wait();
            IndexWriter writer = CreateIndexWriter();
            foreach (var item in list)
            {
                var doc = CreateDoc(item);
                var term = new Term("id", doc.Get("id"));
                writer.UpdateDocument(term, doc);
            }
            writer.Commit();
            writer.Dispose();
            semaphoreSlim.Release();
        }

        public void DeleteById(string id)//根据id删除一条数据
        {
            semaphoreSlim.Wait();
            IndexWriter writer = CreateIndexWriter();
            Term term = new Term("id", id);
            writer.DeleteDocuments(term);
            writer.Commit();
            writer.Dispose();
            semaphoreSlim.Release();
        }

        public void DeleteByIdList(List<string> idlist)//根据id串删除数据
        {
            semaphoreSlim.Wait();
            IndexWriter writer = CreateIndexWriter();
            foreach (var item in idlist)
            {
                Term term = new Term("id", item);
                writer.DeleteDocuments(term);
            }
            writer.Commit();
            writer.Dispose();
            semaphoreSlim.Release();
        }

        public void DeleteAll()//删除所有数据
        {
            semaphoreSlim.Wait();
            IndexWriter writer = CreateIndexWriter();
            writer.DeleteAll();
            writer.Commit();
            writer.Dispose();
            semaphoreSlim.Release();
        }

        public void DeleteByQuery(Query q)//根据查询条件删除
        {
            semaphoreSlim.Wait();
            IndexWriter writer = CreateIndexWriter();
            writer.DeleteDocuments(q);
            writer.Commit();
            writer.Dispose();
            semaphoreSlim.Release();
        }

        public void DeleteByQuery(params Query[] queries)
        {
            semaphoreSlim.Wait();
            IndexWriter writer = CreateIndexWriter();
            writer.DeleteDocuments(queries);
            writer.Commit();
            writer.Dispose();
            semaphoreSlim.Release();
        }

        public void DeleteByTerm(Term t)//根据term删除
        {
            semaphoreSlim.Wait();
            IndexWriter writer = CreateIndexWriter();
            writer.DeleteDocuments(t);
            writer.Commit();
            writer.Dispose();
            semaphoreSlim.Release();
        }

        public void DeleteByTerm(params Term[] terms) //根据term组删除
        {
            semaphoreSlim.Wait();
            IndexWriter writer = CreateIndexWriter();
            writer.DeleteDocuments(terms);
            writer.Commit();
            writer.Dispose();
            semaphoreSlim.Release();
        }

        public int GetCount(Query q)//获取总数
        {
            IndexReader reader = CreateIndexReader();
            IndexSearcher searcher = new IndexSearcher(reader);
            TopDocs topdoc = searcher.Search(q, 1);
            int count = topdoc.TotalHits;
            searcher.Dispose();
            reader.Dispose();
            return count;
        }

        public int MaxDoc()
        {
            IndexReader reader = CreateIndexReader();
            IndexSearcher searcher = new IndexSearcher(reader);
            int max = searcher.MaxDoc;
            searcher.Dispose();
            reader.Dispose();
            return max;
        }

        public T QueryById(string id, string returnFields = null)//根据id查询 //SetBasedFieldSelector、LoadFirstFieldSelector、MapFieldSelector
        {
            IndexReader reader = CreateIndexReader();
            IndexSearcher searcher = new IndexSearcher(reader);
            TermQuery q = new TermQuery(new Term("id", id));

            TopDocs topdoc = searcher.Search(q, 1);

            ScoreDoc[] docs = topdoc.ScoreDocs;

            Document doc = null;
            MapFieldSelector field = null;
            if (returnFields != null)
                field = new MapFieldSelector(returnFields.Split(','));//指定返回列

            foreach (var item in docs)
            {
                if (returnFields != null)
                {
                    doc = searcher.Doc(item.Doc, field);
                }
                else
                {
                    doc = searcher.Doc(item.Doc);
                }
            }

            searcher.Dispose();
            reader.Dispose();

            return CreateModel(doc);
        }

        public void QueryList(QueryInfo<T> queryinfo)
        {
            IndexReader reader = CreateIndexReader();
            IndexSearcher searcher = new IndexSearcher(reader);

            TopDocs topdoc = null;
            if (queryinfo.Sorts == null && queryinfo.Filters == null)
            {
                topdoc = searcher.Search(queryinfo.Querys, queryinfo.ReturnCount);
            }
            if (queryinfo.Sorts == null && queryinfo.Filters != null)
            {
                topdoc = searcher.Search(queryinfo.Querys, queryinfo.Filters, queryinfo.ReturnCount);
            }
            if (queryinfo.Sorts != null)
            {
                topdoc = searcher.Search(queryinfo.Querys, queryinfo.Filters, queryinfo.ReturnCount, queryinfo.Sorts);
            }


            queryinfo.Data = new List<T>();
            queryinfo.Total = topdoc.TotalHits;

            ScoreDoc[] docs = topdoc.ScoreDocs;

            if (queryinfo.Total != 0)
            {
                MapFieldSelector field = null;
                if (queryinfo.ReturnFields != null)
                {
                    field = new MapFieldSelector(queryinfo.ReturnFields.Split(','));//指定返回列
                }

                foreach (var item in docs)
                {
                    Document doc = null;
                    if (queryinfo.ReturnFields != null)
                    {
                        doc = searcher.Doc(item.Doc, field);
                    }
                    else
                    {
                        doc = searcher.Doc(item.Doc);
                    }
                    queryinfo.Data.Add(CreateModel(doc));
                }
            }

            searcher.Dispose();
            reader.Dispose();
        }

        public void QueryList(QueryPageInfo<T> queryinfo)
        {
            IndexReader reader = CreateIndexReader();
            IndexSearcher searcher = new IndexSearcher(reader);

            TopDocs topdoc = null;
            if (queryinfo.Sorts == null && queryinfo.Filters == null)
            {
                topdoc = searcher.Search(queryinfo.Querys, queryinfo.Skip + queryinfo.Take);
            }
            if (queryinfo.Sorts == null && queryinfo.Filters != null)
            {
                topdoc = searcher.Search(queryinfo.Querys, queryinfo.Filters, queryinfo.Skip + queryinfo.Take);
            }
            if (queryinfo.Sorts != null)
            {
                topdoc = searcher.Search(queryinfo.Querys, queryinfo.Filters, queryinfo.Skip + queryinfo.Take, queryinfo.Sorts);
            }

            queryinfo.Data = new List<T>();
            queryinfo.Total = topdoc.TotalHits;

            ScoreDoc[] docs = topdoc.ScoreDocs;

            if (queryinfo.Total != 0)
            {
                MapFieldSelector field = null;
                if (queryinfo.ReturnFields != null)
                {
                    field = new MapFieldSelector(queryinfo.ReturnFields.Split(','));//指定返回列
                }
                int end = queryinfo.Skip + queryinfo.Take;

                if (queryinfo.Skip <= queryinfo.Total)
                {
                    if (queryinfo.Total <= end)
                    {
                        end = queryinfo.Total;
                    }

                    for (int i = queryinfo.Skip; i < end; i++)
                    {
                        Document doc = null;
                        if (queryinfo.ReturnFields != null)
                        {
                            doc = searcher.Doc(docs[i].Doc, field);
                        }
                        else
                        {
                            doc = searcher.Doc(docs[i].Doc);
                        }
                        queryinfo.Data.Add(CreateModel(doc));
                    }
                }
            }


            searcher.Dispose();
            reader.Dispose();
        }


    }
}
