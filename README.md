# LuceneHelper
Lucene帮助类库.采用单例IndexWriter进行写操作，构造全局IndexReader，当IndexWriter变动时对IndexReader进行ReOpen。大大提高索引和搜索速度。
采用FasterMember.dll，快速反射得到实体。采用ORM类似方式，对索引进行存储和读取。

//对people索引增删改等操作
 using (var conn = new LuceneConnection("people"))
  {
      People p = new People();
      p.Name = "张三";

      conn.Insert(p); //添加索引
      conn.DeleteById("1");//删除索引
      //conn.更多方法

      conn.SaveChanges(); //提交
  }
