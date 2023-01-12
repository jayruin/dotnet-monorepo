using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Testing;

namespace Databases.Tests;

[TestClass]
public class SqliteTests
{
    [TestMethod]
    public void TestReadWriteRows()
    {
        using TempFile tempFile = new();
        string connectionString = $"Data Source={tempFile.FilePath};";
        IDatabaseClient databaseClient = new DatabaseClient
        {
            DefaultProvider = DatabaseProvider.Sqlite,
            DefaultConnectionString = connectionString,
        };
        databaseClient.ExecuteNonQuery($"CREATE TABLE TESTTABLE (ID INT PRIMARY KEY NOT NULL, VALUE TEXT NOT NULL);");
        databaseClient.ExecuteNonQuery($"INSERT INTO TESTTABLE (ID, VALUE) VALUES ({1}, {"a"})");
        databaseClient.ExecuteNonQuery($"INSERT INTO TESTTABLE (ID, VALUE) VALUES ({2}, {"b"})");
        List<string> results = databaseClient
            .ExecuteQuery($"SELECT * FROM TESTTABLE", row => $"{row.GetValue<int>(0)}{row.GetValue<string>(1)}")
            .ToList();
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("1a", results[0]);
        Assert.AreEqual("2b", results[1]);
    }

    [TestMethod]
    public async Task TestReadWriteRowsAsync()
    {
        using TempFile tempFile = new();
        string connectionString = $"Data Source={tempFile.FilePath};";
        IDatabaseClient databaseClient = new DatabaseClient
        {
            DefaultProvider = DatabaseProvider.Sqlite,
            DefaultConnectionString = connectionString,
        };
        await databaseClient.ExecuteNonQueryAsync($"CREATE TABLE TESTTABLE (ID INT PRIMARY KEY NOT NULL, VALUE TEXT NOT NULL);");
        await databaseClient.ExecuteNonQueryAsync($"INSERT INTO TESTTABLE (ID, VALUE) VALUES ({1}, {"a"})");
        await databaseClient.ExecuteNonQueryAsync($"INSERT INTO TESTTABLE (ID, VALUE) VALUES ({2}, {"b"})");
        List<string> results = new();
        IAsyncEnumerable<string> queryResults = databaseClient
            .ExecuteQueryAsync($"SELECT * FROM TESTTABLE", async row => $"{await row.GetValueAsync<int>(0)}{await row.GetValueAsync<string>(1)}");
        await foreach (string queryResult in queryResults)
        {
            results.Add(queryResult);
        }
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("1a", results[0]);
        Assert.AreEqual("2b", results[1]);
    }
}
