using GCHeritagePlatformCoreApi.Infrastructure;
using GCHeritagePlatformCoreApi.PanGuLucene.Common;
using GCHeritagePlatformCoreApi.PanGuLucene.Model;
using GCHeritagePlatformCoreApi.Repository.Extension;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using PanGu;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GCHeritagePlatformCoreApi.PanGuLucene
{
    public  class SearchHelper
    {
        /// <summary>
        /// 创建索引
        /// </summary>
        /// <returns></returns>
        public static ResultModel CreateIndex()
        {
            try
            {
                //索引保存位置
                //var indexPath = AppDomain.CurrentDomain.BaseDirectory + "/PanGuIndex";
                var indexPath = System.IO.Directory.GetCurrentDirectory() + "/PanGuIndex/YqIndex";
                if (!System.IO.Directory.Exists(indexPath)) System.IO.Directory.CreateDirectory(indexPath);
                //indexPath = Path.Combine(indexPath, "PanGuIndex.txt");
                //FSDirectory directory = FSDirectory.Open(new DirectoryInfo(indexPath), new NativeFSLockFactory());

                var directory = FSDirectory.Open(new DirectoryInfo(indexPath), new NativeFSLockFactory());
                //var analyzer = new JieBaAnalyzer(TokenizerMode.Search);
                var boostList =new SearchHelper().GetBoostList();
                //if (IndexWriter.IsLocked(directory))
                //{
                //    //  如果索引目录被锁定（比如索引过程中程序异常退出），则首先解锁
                //    //  Lucene.Net在写索引库之前会自动加锁，在close的时候会自动解锁
                //    IndexWriter.Unlock(directory);
                //}
                //Lucene的index模块主要负责索引的创建
                //  创建向索引库写操作对象  IndexWriter(索引目录,指定使用盘古分词进行切词,最大写入长度限制)
                //  补充:使用IndexWriter打开directory时会自动对索引库文件上锁
                //IndexWriter构造函数中第一个参数指定索引文件存储位置；
                //第二个参数指定分词Analyzer，Analyzer有多个子类，
                //然而其分词效果并不好，这里使用的是第三方开源分词工具盘古分词；
                //第三个参数表示是否重新创建索引，true表示重新创建（删除之前的索引文件），
                //最后一个参数指定Field的最大数目。
                var analyzer = new PanGuAnalyzer();
                //var indexWriterConfig = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyzer);

                //new PanGuAnalyzer();分词
                using (IndexWriter writer = new IndexWriter(directory, new PanGuAnalyzer(), true,
                     IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    var dbContext = new BaseRepository();
                    var sql = "select id,BT,YW from hpf_yq limit 2000";
                    var dt = dbContext.GetDataTableResult(sql);

                    foreach (DataRow item in dt.Rows)
                    {
                        //  一条Document相当于一条记录
                        Document document = new Document();
                        //  每个Document可以有自己的属性（字段），所有字段名都是自定义的，值都是string类型
                        //  Field.Store.YES不仅要对文章进行分词记录，也要保存原文，就不用去数据库里查一次了
                        var field = new Field("ID", item["ID"] + "", Field.Store.YES, Field.Index.NOT_ANALYZED);

                        document.Add(field);
                        field = new Field("Title", item["BT"] + "", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS);
                        if (boostList.Count(e => (item["BT"] + "").Contains(e)) > 0)
                        {
                            field.Boost = 2f;
                        }
                        document.Add(field);
                        //  需要进行全文检索的字段加 Field.Index. ANALYZED
                        //  Field.Index.ANALYZED:指定文章内容按照分词后结果保存，否则无法实现后续的模糊查询 
                        //  WITH_POSITIONS_OFFSETS:指示不仅保存分割后的词，还保存词之间的距离
                        //document.Add(new Field("Content", item["YW"] + "", Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
                        field = new Field("Content", item["YW"] + "", Field.Store.YES, Field.Index.ANALYZED_NO_NORMS, Field.TermVector.WITH_POSITIONS_OFFSETS);
                        if (boostList.Count(e => (item["YW"] + "").Contains(e)) > 0)
                        {
                            field.Boost = 1.5f;
                        }
                        document.Add(field);


                        writer.AddDocument(document);
                    }
                    writer.Commit();//提交
                    //writer.Close(); // Close后自动对索引库文件解锁
                    directory.Dispose(); //  不要忘了Close，否则索引结果搜不到
                }
                var value = JsonHelper.SerializeObject(new ResultModel(true, "索引创建完毕！"));
                return new ResultModel(true, "索引创建完毕！");
            }
            catch (Exception ex)
            {
                return new ResultModel(false, $"索引创建失败:{ex.Message}");
            }

        }

        /// <summary>
        /// 盘古分词
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        public static string AnalyzerResult(string words)
        {
            var str = Participle.AnalyzerResult(words);
            return str;
        }
        /// <summary>
        /// 搜索
        /// </summary>
        /// <returns></returns>
        public static ResultModel Search(string keyWord, int pageIndex, int pageSize)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                //string indexPath = AppDomain.CurrentDomain.BaseDirectory + "/Index";
                var indexPath = System.IO.Directory.GetCurrentDirectory() + "/PanGuIndex/YqIndex";
                if (!System.IO.Directory.Exists(indexPath)) System.IO.Directory.CreateDirectory(indexPath);
                FSDirectory directory = FSDirectory.Open(new DirectoryInfo(indexPath), new NativeFSLockFactory());
                IndexReader reader = IndexReader.Open(directory, true);
                //创建IndexSearcher准备进行搜索。
                IndexSearcher searcher = new IndexSearcher(reader);
                // 查询条件
                keyWord = GetKeyWordsSplitBySpace(keyWord, new PanGuTokenizer());
                //创建QueryParser查询解析器。用来对查询语句进行语法分析。
                //QueryParser调用parser进行语法分析，形成查询语法树，放到Query中。
                QueryParser msgQueryParser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, new string[] { "Title", "Content" }, new PanGuAnalyzer());
                //QueryParser queryParser=new 
                Query msgQuery = msgQueryParser.Parse(keyWord);
                //HashMap<String, String> map = new HashMap<String, String>();
                //if (BootListData.Contains(keyWord))
                //{
                //    //设置权重
                //    msgQuery.Boost = 2.0f;
                //}
                //TopScoreDocCollector:盛放查询结果的容器
                //numHits 获取条数
                TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, true);
                //IndexSearcher调用search对查询语法树Query进行搜索，得到结果TopScoreDocCollector。
                // 使用query这个查询条件进行搜索，搜索结果放入collector
                searcher.Search(msgQuery, collector);
                // 从查询结果中取出第n条到第m条的数据
                var startIndex = (pageIndex - 1) * pageSize;
                ScoreDoc[] docs = collector.TopDocs(startIndex, pageSize).ScoreDocs;
                stopwatch.Stop();
                // 遍历查询结果
                List<ReturnModel> dataList = new List<ReturnModel>();
                var total = collector.TotalHits;
                var result = new PageModel<ReturnModel>
                {
                    Total = total
                };
                for (int i = 0; i < docs.Length; i++)
                {
                    var doc = searcher.Doc(docs[i].Doc);
                    var content = HighLightHelper.HighLight(keyWord, doc.Get("Content"));
                    var title = HighLightHelper.HighLight(keyWord, doc.Get("Title"));
                    var itemData = new ReturnModel
                    {
                        ID = doc.Get("ID"),
                        Title = title,
                        Content = content,
                        Score = docs[i].Score
                        //Count = Regex.Matches(content, "<font").Count+Regex.Matches(title, "<font").Count
                    };
                    dataList.Add(itemData);
                }

                result.DataList = dataList;
                var elapsedTime = stopwatch.ElapsedMilliseconds + "ms";
                return new ResultModel(true, result, elapsedTime);
            }
            catch (Exception ex)
            {
                return new ResultModel(false, $"查询失败啦:{ex.Message}");
            }

        }
        /// <summary>
        /// 对关键字进行盘古分词处理
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="ktTokenizer"></param>
        /// <returns></returns>
        private static string GetKeyWordsSplitBySpace(string keywords, PanGuTokenizer ktTokenizer)
        {
            StringBuilder result = new StringBuilder();
            ICollection<WordInfo> words = ktTokenizer.SegmentToWordInfos(keywords);

            foreach (PanGu.WordInfo word in words)
            {
                if (word == null)
                {
                    continue;
                }
                result.AppendFormat("{0}^{1}.0 ", word.Word, (int)Math.Pow(3, word.Rank));
            }
            return result.ToString().Trim();
        }

        public  List<string> GetBoostList()
        {
            var boostListData = new List<string>();
            boostListData.Add("文化遗产");
            return boostListData;
        }

        /// <summary>
        /// 删除索引数据（根据id）
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool Delete(string id)
        {
            bool IsSuccess = false;
            Term term = new Term("id", id);
            //Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);
            //Version version = new Version();
            //MultiFieldQueryParser parser = new MultiFieldQueryParser(version, new string[] { "name", "job" }, analyzer);//多个字段查询
            //Query query = parser.Parse("小王");

            //IndexReader reader = IndexReader.Open(directory_luce, false);
            //reader.DeleteDocuments(term);
            //Response.Write("删除记录结果： " + reader.HasDeletions + "<br/>");
            //reader.Dispose();
            var indexPath = System.IO.Directory.GetCurrentDirectory() + "/YqIndex";
            var directory = FSDirectory.Open(new DirectoryInfo(indexPath));
            IndexWriter writer = new IndexWriter(directory, new PanGuAnalyzer(), false, IndexWriter.MaxFieldLength.LIMITED);
            writer.DeleteDocuments(term); // writer.DeleteDocuments(term)或者writer.DeleteDocuments(query);
            writer.DeleteAll();
            writer.Commit();
            //writer.Optimize();//
            IsSuccess = writer.HasDeletions();
            writer.Dispose();
            return IsSuccess;
        }
        /// <summary>
        /// 删除全部索引数据
        /// </summary>
        /// <returns></returns>
        public static bool DeleteAll()
        {
            bool IsSuccess = true;
            try
            {
                var indexPath = System.IO.Directory.GetCurrentDirectory() + "/YqIndex";
                var directory = FSDirectory.Open(new DirectoryInfo(indexPath));
                IndexWriter writer = new IndexWriter(directory, new PanGuAnalyzer(), false, IndexWriter.MaxFieldLength.LIMITED);
                writer.DeleteAll();
                writer.Commit();
                //writer.Optimize();//
                IsSuccess = writer.HasDeletions();
                writer.Dispose();
            }
            catch
            {
                IsSuccess = false;
            }
            return IsSuccess;
        }
    }
}