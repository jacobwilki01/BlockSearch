using System.Diagnostics;
using BlockSearch.Database;
using BlockSearch.Internal.Data;
using BlockSearch.Internal.Util;

BlockSearchDatabase database = new BlockSearchDatabase();
database.Initialize();
database.ResetDatabase();
database.Initialize();

Stopwatch watch = new();
watch.Start();
List<Document> docs = FileProcessor.ProcessDirectory("/Users/jacobwilkus/Documents/Testing");
database.ProcessDocuments(ref docs);
watch.Stop();

TimeSpan ts = watch.Elapsed;
string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
Console.WriteLine("Function execution time: " + elapsedTime);

while (true) {}