using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using KeClientTracing.LogIndexing;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace LogManagerService.DbLayer
{
    public class MongoDb: IDb
    {
        public void RemoveIndexEntires(DateTime date)
        {
            MongoCollection<BsonDocument> items = GetItemsMongoCollection();
            var query = Query.EQ("date", DateTime.SpecifyKind(date, DateTimeKind.Utc));
            SafeModeResult smr = items.Remove(query, SafeMode.True);
            Console.WriteLine(smr.DocumentsAffected + " entries deleted from generic collection");
            MongoCollection<BsonDocument> itemsPerDate = GetMongoCollectionForDate(date);
            if (itemsPerDate != null)
                itemsPerDate.Drop();
        }

        public void SaveIndexEntries(DateTime date, Stream stream)
        {
            MongoCollection<BsonDocument> items = GetMongoCollectionForDate(date, true);
            
            items.EnsureIndex(IndexKeys.Ascending("inn"));

            StreamReader streamReader = new StreamReader(stream);
            string line;
            List<BsonDocument> batch = new List<BsonDocument>();
            int cnt = 0;
            while ((line = streamReader.ReadLine()) != null)
            {
                cnt++;

                BsonDocument item = GetBsonDocument(line);
                batch.Add(item);
                if (cnt % 100000 == 0)
                {
                    Console.WriteLine("Batch "+cnt+" added");
                    items.InsertBatch(batch);
                    batch.Clear();
                }
            }
            items.InsertBatch(batch);
        }

        public FindResult Find(List<Condition> conditions)
        {

            Dictionary<string, QueryBuilder> queryBuilder = new Dictionary<string, QueryBuilder>();
            foreach (Condition condition in conditions)
            {
                AddCondition(queryBuilder, condition);
            }
            var from =
                DateTime.SpecifyKind(
                    DateTime.Parse(
                        conditions.Where(c => c.Name == "datebegin")
                                  .Select(c => c.Value)
                                  .DefaultIfEmpty("01.01.2010")
                                  .First()), DateTimeKind.Utc);
            var to =
                DateTime.SpecifyKind(
                    DateTime.Parse(
                        conditions.Where(c => c.Name == "dateend")
                                  .Select(c => c.Value)
                                  .DefaultIfEmpty(DateTime.Now.Date.ToString(CultureInfo.InvariantCulture))
                                  .First()), DateTimeKind.Utc);

            var days = Enumerable.Range(0, (int)Math.Floor((to - from).TotalDays)).ToList();
            IMongoQuery completeQuery = BuildQuery(queryBuilder);
            var tasks = new Task<List<FindResultEntry>>[days.Count];
            days.ForEach(d =>
                {
                    var localD = d;
                    tasks[localD] = Task<List<FindResultEntry>>.Factory.StartNew(() =>
                    {
                        MongoCollection<BsonDocument> items = GetMongoCollectionForDate(from.AddDays(localD));
                        List<FindResultEntry> results = new List<FindResultEntry>();
                        if (items == null)
                            return results;
                        var found = items.Find(completeQuery);

                        foreach (BsonDocument bsonDocument in found)
                        {
                            //Guid id = new Guid(bsonDocument["_id"].AsObjectId + "00000000");
                            string id = bsonDocument["_id"].AsObjectId.ToString();
                            DateTime date = bsonDocument["date"].AsDateTime;
                            string host = bsonDocument["host"].AsString;
                            string ip = bsonDocument["ip"].AsString;
                            string inn = bsonDocument["inn"].AsString;
                            string sessionId = bsonDocument["sessionId"].AsString;
                            TimeSpan sessionStart = new TimeSpan(bsonDocument["sessionStart"].AsInt64);
                            TimeSpan sessionEnd = new TimeSpan(bsonDocument["sessionEnd"].AsInt64);
                            results.Add(new FindResultEntry(id, date, host, ip, inn, sessionId, sessionStart, sessionEnd));
                        }
                        return results;
                    });
                }
                    );
            Task.WaitAll(tasks);
            return new FindResult(tasks.SelectMany(t => t.Result).ToList());
        }

        private MongoCollection<BsonDocument> GetItemsMongoCollection()
        {
            return GetMongoCollection("Items", true);
        }

        private MongoCollection<BsonDocument> GetMongoCollectionForDate(DateTime date, bool create = false)
        {
            return GetMongoCollection(String.Format("{0:0000}{1:00}{2:00}", date.Year, date.Month, date.Day), create);
        }

        private MongoCollection<BsonDocument> GetMongoCollection(string collectionName, bool create = false)
        {
            string[] connstring = File.ReadAllLines(@"settings\WeblogIndexMongoDbConnectionString");
            MongoUrlBuilder builder = new MongoUrlBuilder(connstring[0]);
            builder.SocketTimeout = new TimeSpan(0, 30, 0);
            //builder.Server = port.HasValue ? new MongoServerAddress(host, port.Value) : new MongoServerAddress(host);
            MongoServer server = MongoServer.Create(builder.ToServerSettings());
            server.Connect();
            MongoDatabase webLogIndex = server.GetDatabase("WebLogIndex");
            if (webLogIndex.CollectionExists(collectionName) || create)
                return webLogIndex.GetCollection(collectionName);
            return null;
        }



        private IMongoQuery BuildQuery(Dictionary<string, QueryBuilder> queryBuilder)
        {
            queryBuilder.Remove("datebegin");
            queryBuilder.Remove("dateend");
            List<QueryComplete> conditionList = new List<QueryComplete>();
            foreach (QueryBuilder value in queryBuilder.Values)
            {
                conditionList.Add(value as QueryComplete);
            }
            return Query.And(conditionList.ToArray());
        }

        private void AddCondition(Dictionary<string, QueryBuilder> queryBuilder, Condition condition)
        {
            if (queryBuilder.ContainsKey(condition.Name))
                return;

            QueryBuilder qb;
            switch (condition.Name)
            {
                case "datebegin":
                    {
                        queryBuilder.Add("datebegin", null);
                        if (queryBuilder.ContainsKey("dateend"))
                        {
                            qb =
                                (queryBuilder["date"] as QueryConditionList).GTE(
                                    DateTime.SpecifyKind(DateTime.Parse(condition.Value), DateTimeKind.Utc));
                            queryBuilder["date"] = qb;
                        }
                        else
                        {
                            qb = Query.GTE("date",
                                           DateTime.SpecifyKind(DateTime.Parse(condition.Value), DateTimeKind.Utc));
                            queryBuilder.Add("date", qb);
                        }
                        break;

                    }

                case "dateend":
                    {
                        queryBuilder.Add("dateend", null);
                        if (queryBuilder.ContainsKey("datebegin"))
                        {
                            qb =
                                (queryBuilder["date"] as QueryConditionList).LTE(
                                    DateTime.SpecifyKind(DateTime.Parse(condition.Value), DateTimeKind.Utc));
                            queryBuilder["date"] = qb;
                        }
                        else
                        {
                            qb = Query.LTE("date",
                                           DateTime.SpecifyKind(DateTime.Parse(condition.Value), DateTimeKind.Utc));
                            queryBuilder.Add("date", qb);
                        }
                        break;
                        break;
                    }
                default:
                    {
                        if (condition.Value.EndsWith("*"))
                            qb = Query.Matches(condition.Name,
                                               new BsonRegularExpression(String.Format("/^{0}/", condition.Value.TrimEnd('*'))));
                        else
                            qb = Query.EQ(condition.Name, condition.Value);
                        queryBuilder.Add(condition.Name, qb);

                        break;
                    }
            }

        }

        public LogDataPlacementDescription GetLogDataPlacementDescription(string entryId)
        {

            DateTime date = DateTime.MinValue;
            long offset = 0;
            long length = 0;
            //string entryIdStr = entryId.ToString();//.Replace("-", "").Remove(24);
            MongoCollection<BsonDocument> items = GetItemsMongoCollection();
            IMongoQuery query = Query.EQ("_id", new BsonObjectId(entryId));
            var doc = items.Find(query);
            if (doc.Count() == 0)
                return null;
            IEnumerator<BsonDocument> enumerator = doc.GetEnumerator();
            enumerator.MoveNext();
            date = enumerator.Current["date"].AsDateTime;
            offset = enumerator.Current["startLogPos"].AsInt64;
            length = enumerator.Current["endLogPos"].AsInt64;


            return new LogDataPlacementDescription(date, offset, length);
        }

        

        private static BsonDocument GetBsonDocument(string logLine)
        {
            string[] data = logLine.Split('\t');
            DateTime date = IndexLineHelper.GetDate(data);
            string host = IndexLineHelper.GetHost(data);
            string ip = IndexLineHelper.GetIP(data);
            string inn = IndexLineHelper.GetINN(data);
            string sessionId = IndexLineHelper.GetSessionId(data);
            long startLogPos = IndexLineHelper.GetStartLogPos(data);
            long endLogPos = IndexLineHelper.GetEndLogPos(data);
            TimeSpan startTime = IndexLineHelper.GetSessionStartTime(data);
            TimeSpan endTime = IndexLineHelper.GetSessionEndTime(data); 
            
            return new BsonDocument
                       {
                           {"date", date },
                           {"host", host},
                           {"ip", ip},
                           {"inn", inn},
                           {"sessionId", sessionId},
                           {"startLogPos", startLogPos},
                           {"endLogPos", endLogPos},
                           {"sessionStart",  startTime.Ticks},
                           {"sessionEnd", endTime.Ticks}
                       };
        }

    }

 
}