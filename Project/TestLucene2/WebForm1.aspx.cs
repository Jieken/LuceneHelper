using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;


namespace TestLucene2
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            using (var conn = new LuceneConnection("people"))
            {
                People p = new People();
                p.Name = "张三";

                conn.Insert(p); //添加索引
                conn.DeleteById("1");//删除索引
                //conn.更多方法

                conn.SaveChanges(); //提交
            }
        }
    }
}