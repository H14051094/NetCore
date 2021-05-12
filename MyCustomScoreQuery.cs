using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using System;
using System.Collections.Generic;
using System.Text;

namespace GCHeritagePlatformCoreApi.PanGuLucene
{
    //自定义评分的思路是：
    //1. 创建一个类继承于CustomScoreQuery
    //2. override getCustomScoreProvider方法
    //3. 创建CustomScoreProvider类
    //4. override customScore方法

    public class MyCustomScoreQuery: CustomScoreQuery
    {
        public MyCustomScoreQuery(Query subQuery, params ValueSourceQuery[] valSrcQueries) :base(subQuery, valSrcQueries)
        {
        }
        protected override CustomScoreProvider GetCustomScoreProvider(IndexReader reader)
        {
            return new MyCustomScoreProvider(reader);
        }
    }
    public class MyCustomScoreProvider : CustomScoreProvider
    {
        public MyCustomScoreProvider(IndexReader reader) : base(reader)
        { 
            
        }
        public override float CustomScore(int doc, float subQueryScore, float[] valSrcScores)
        {
            if (valSrcScores.Length == 1)
            {
                return CustomScore(doc, subQueryScore, valSrcScores[0]);
            }

            if (valSrcScores.Length == 0)
            {
                return CustomScore(doc, subQueryScore, 1f);
            }

            float num = subQueryScore;
            for (int i = 0; i < valSrcScores.Length; i++)
            {
                num *= valSrcScores[i];
            }

            return num;
        }
    }
}
