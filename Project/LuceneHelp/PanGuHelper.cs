using Lucene.Net.Analysis;
using Lucene.Net.Analysis.MMSeg;
using Lucene.Net.Analysis.PanGu;
using PanGu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LuceneHelp
{
    public class PanGuHelper
    {
        public static string[] SplitWords(string content)
        {
            List<string> strList = new List<string>();
            Analyzer analyzer = new PanGuAnalyzer();//指定使用盘古 PanGuAnalyzer 分词算法

            TokenStream tokenStream = analyzer.TokenStream("", new StringReader(content));

            Lucene.Net.Analysis.Tokenattributes.ITermAttribute ita;

            bool hasNext = tokenStream.IncrementToken();

            while (hasNext)
            {
                ita = tokenStream.GetAttribute<Lucene.Net.Analysis.Tokenattributes.ITermAttribute>();
                strList.Add(ita.Term);
                hasNext = tokenStream.IncrementToken();
            }
            
            return strList.ToArray();
        }

        public static string[] SplitWords2(string content)
        {
            List<string> strList = new List<string>();
            Analyzer analyzer = new Lucene.Net.Analysis.SimpleAnalyzer();//指定使用盘古 PanGuAnalyzer 分词算法

            TokenStream tokenStream = analyzer.TokenStream("", new StringReader(content));

            Lucene.Net.Analysis.Tokenattributes.ITermAttribute ita;

            bool hasNext = tokenStream.IncrementToken();

            while (hasNext)
            {
                ita = tokenStream.GetAttribute<Lucene.Net.Analysis.Tokenattributes.ITermAttribute>();
                strList.Add(ita.Term);
                hasNext = tokenStream.IncrementToken();
            }

            return strList.ToArray();
        }

        public static string QueryParserWord(string content)
        {
            StringBuilder sb = new StringBuilder();
            Analyzer analyzer = new PanGuAnalyzer();//指定使用盘古 PanGuAnalyzer 分词算法

            TokenStream tokenStream = analyzer.TokenStream("", new StringReader(content));

            Lucene.Net.Analysis.Tokenattributes.ITermAttribute ita;

            bool hasNext = tokenStream.IncrementToken();

            while (hasNext)
            {
                ita = tokenStream.GetAttribute<Lucene.Net.Analysis.Tokenattributes.ITermAttribute>();

                sb.Append(ita.Term);
                sb.Append(" ");

                hasNext = tokenStream.IncrementToken();
            }

            return sb.ToString();
        }

        //需要添加PanGu.HighLight.dll的引用
        /// <summary>
        /// 搜索结果高亮显示
        /// </summary>
        /// <param name="keyword"> 关键字 </param>
        /// <param name="content"> 搜索结果 </param>
        /// <returns> 高亮后结果 </returns>
        public static string HightLight(string keyword, string content)
        {
            //创建HTMLFormatter,参数为高亮单词的前后缀
            PanGu.HighLight.SimpleHTMLFormatter simpleHTMLFormatter =
                new PanGu.HighLight.SimpleHTMLFormatter("<font style=\"font-style:normal;color:#cc0000;\"><b>", "</b></font>");
            //创建 Highlighter ，输入HTMLFormatter 和 盘古分词对象Semgent
            PanGu.HighLight.Highlighter highlighter =
                            new PanGu.HighLight.Highlighter(simpleHTMLFormatter,new Segment());
            //设置每个摘要段的字符数
            highlighter.FragmentSize = 1000;
            //获取最匹配的摘要段
            return highlighter.GetBestFragment(keyword, content);
        }
    }
}
