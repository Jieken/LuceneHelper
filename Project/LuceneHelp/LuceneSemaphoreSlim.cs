using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace LuceneHelp
{
    /// <summary>
    /// 此类用于Lucene写操作时进行锁定，限定只允许一个县城对索引进行操作
    /// </summary>
    public class LuceneSemaphoreSlim
    {
        private static Dictionary<string, SemaphoreSlim> dir = new Dictionary<string, SemaphoreSlim>();//定义全局信号量
        private static object locker = new object();

        public static SemaphoreSlim GetSemaphore(string name)
        {
            lock (locker)
            {
                if (!dir.Keys.Contains(name))
                {
                    dir.Add(name, new SemaphoreSlim(1));
                }
                return dir[name];
            }
        }
    }
}