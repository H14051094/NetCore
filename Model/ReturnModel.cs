using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GCHeritagePlatformCoreApi.PanGuLucene.Model
{
    public class ReturnModel
    {
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }
        public string ID { get; set; }
        /// <summary>
        /// 内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 出现次数
        /// </summary>
        public int? Count { get; set; }
        /// <summary>
        /// 得分
        /// </summary>
        public float? Score { get; set; }
    }
}