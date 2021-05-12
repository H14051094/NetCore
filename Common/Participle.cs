using Lucene.Net.Analysis;
using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GCHeritagePlatformCoreApi.PanGuLucene.Common
{
    public class Participle
    {
        /// <summary>
        /// 盘古分词
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        public static string AnalyzerResult(string words)
        {
            if (string.IsNullOrWhiteSpace(words)) return "";
            /*Lucene.Net.Analysis.Standard.StandardAnalyzer analyzer = new Lucene.Net.Analysis.Standard.StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);*/
            Analyzer analyzer = new PanGuAnalyzer();
            System.IO.StringReader reader = new System.IO.StringReader(words);
            Lucene.Net.Analysis.TokenStream ts = analyzer.TokenStream("", reader);
            bool hasnext = ts.IncrementToken();
            Lucene.Net.Analysis.Tokenattributes.ITermAttribute ita;
            var str = "";
            while (hasnext)
            {
                ita = ts.GetAttribute<Lucene.Net.Analysis.Tokenattributes.ITermAttribute>();
                str += ita.Term + "   |  ";
                hasnext = ts.IncrementToken();
            }
            ts.CloneAttributes();
            reader.Close();
            analyzer.Close();
            return str.TrimEnd().TrimEnd('|');
        }
      
    }
}
