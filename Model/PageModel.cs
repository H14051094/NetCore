using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GCHeritagePlatformCoreApi.PanGuLucene.Model
{
    public class PageModel<T>
    {
        /// <summary>
        /// 总数
        /// </summary>
        public int? Total { get; set; }
        /// <summary>
        /// 集合
        /// </summary>
        public List<T> DataList { get; set; }
    }
}