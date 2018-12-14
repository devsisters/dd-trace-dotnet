using System;
using Datadog.Trace.ClrProfiler;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Samples.MongoDB
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"Profiler attached: {Instrumentation.ProfilerAttached}");
            Console.WriteLine($"Platform: {(Environment.Is64BitProcess ? "x64" : "x32")}");

            var allFilter = new BsonDocument();

            var newDocument = new BsonDocument
            {
                { "name", "MongoDB" },
                { "type", "Database" },
                { "count", 1 },
                { "info", new BsonDocument
                    {
                        { "x", 203 },
                        { "y", 102 }
                    }
                }
            };

            var client = new MongoClient();
            var database = client.GetDatabase("test-db");
            var collection = database.GetCollection<BsonDocument>("employees");

            collection.DeleteMany(allFilter);
            collection.InsertOne(newDocument);

            var count = collection.Count(new BsonDocument());
            Console.WriteLine($"Documents: {count}");

            var allDocuments = collection.Find(allFilter).ToList();

            Console.WriteLine(newDocument == allDocuments[0]);

            Console.WriteLine("Press [ENTER] to exit...");
            Console.ReadLine();
        }
    }
}
