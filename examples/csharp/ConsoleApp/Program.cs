using System;
using TableStorageLocal;
using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;

namespace csharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var tables = new LocalTables(10002);
            var client = CloudStorageAccount.Parse(tables.ConnectionString).CreateCloudTableClient();
            var table = client.GetTableReference("test");
            table.CreateIfNotExists();
            var entity = new DynamicTableEntity("PK", "RK", "*", new Dictionary<string, EntityProperty>() { { "Message", EntityProperty.GeneratePropertyForString("Hello, World!") } });
            table.Execute(TableOperation.Insert(entity));
            var result = table.Execute(TableOperation.Retrieve("PK", "RK"));
            Console.WriteLine(((DynamicTableEntity)result.Result).Properties["Message"]);
            Console.ReadLine();
        }
    }
}
