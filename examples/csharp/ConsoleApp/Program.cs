using System;
using FakeAzureTables;
using Microsoft.Azure.Cosmos.Table;

namespace csharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var tables = new FakeTables();
            var client = CloudStorageAccount.Parse(tables.ConnectionString).CreateCloudTableClient();
            var table = client.GetTableReference("test");
            table.CreateIfNotExists();

            Console.WriteLine($"ConnectionString is {tables.ConnectionString}");
            Console.ReadLine();
        }
    }
}
