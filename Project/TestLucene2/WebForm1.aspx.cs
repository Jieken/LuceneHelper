using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Lucene.Net.Search;
using LuceneHelp;
using System.Web.Script.Serialization;
using Lucene.Net.Index;

namespace TestLucene2
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }
        JavaScriptSerializer jss = new JavaScriptSerializer();
        LuceneHelper<People> _PeopleDAL = new LuceneHelper<People>();
        //new MatchAllDocsQuery();
        protected void Button1_Click(object sender, EventArgs e)
        {


            List<People> list = new List<People>();
            for (int i = 1; i < 1000000; i++)
            {
                People p = new People();
                p.id = i.ToString();
                p.Name = "张三" + i;
                p.Phone = 10000 + i;
                p.Money = 0.0M + 1;
                list.Add(p);
            }
            _PeopleDAL.InsertList(list);
            _PeopleDAL.Dispose();
        }

        protected void Button2_Click(object sender, EventArgs e)
        {

            People p = new People();
            p.id = "1";
            p.Name = "哈哈";
            p.Phone = 110;
            p.Money = 1000000M;
            _PeopleDAL.Update(p);


            QueryPageInfo<People> queryinfo = new QueryPageInfo<People>();
            queryinfo.Skip = 0;
            queryinfo.Take = 5;
            queryinfo.ReturnFields = "id,Name";
           // queryinfo.Querys = new TermQuery(new Term("id","1"));
            queryinfo.Querys = new MatchAllDocsQuery();
            _PeopleDAL.QueryList(queryinfo);

            Response.Write(queryinfo.Total);
            Response.Write(jss.Serialize(queryinfo.Data.Select(s => new { id = s.id, name = s.Name })));

            _PeopleDAL.Dispose();
            
        }
    }
}