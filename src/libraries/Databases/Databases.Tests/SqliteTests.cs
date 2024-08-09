using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Temp;

namespace Databases.Tests;

[TestClass]
public class SqliteTests
{
    [TestMethod]
    public void TestReadWriteRows()
    {
        using TempFile tempFile = new();
        SqliteDataSourceFactory dataSourceFactory = new();
        DbDataSource dataSource = dataSourceFactory.CreateDataSource(tempFile.FilePath, SqliteOpenMode.ReadWriteCreate);
        dataSource.ExecuteCommand($"CREATE TABLE TESTTABLE (ID INT PRIMARY KEY NOT NULL, VALUE TEXT NOT NULL);");
        dataSource.ExecuteCommand($"INSERT INTO TESTTABLE (ID, VALUE) VALUES ({1}, {"a"})");
        dataSource.ExecuteCommand($"INSERT INTO TESTTABLE (ID, VALUE) VALUES ({2}, {"b"})");
        List<string> results = dataSource.ExecuteCommand($"SELECT * FROM TESTTABLE",
                row => $"{row.GetValue<int>(0)}{row.GetValue<string>(1)}")
            .ToList();
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("1a", results[0]);
        Assert.AreEqual("2b", results[1]);
    }

    [TestMethod]
    public async Task TestReadWriteRowsAsync()
    {
        using TempFile tempFile = new();
        SqliteDataSourceFactory dataSourceFactory = new();
        DbDataSource dataSource = dataSourceFactory.CreateDataSource(tempFile.FilePath, SqliteOpenMode.ReadWriteCreate);
        await dataSource.ExecuteCommandAsync($"CREATE TABLE TESTTABLE (ID INT PRIMARY KEY NOT NULL, VALUE TEXT NOT NULL);");
        await dataSource.ExecuteCommandAsync($"INSERT INTO TESTTABLE (ID, VALUE) VALUES ({1}, {"a"})");
        await dataSource.ExecuteCommandAsync($"INSERT INTO TESTTABLE (ID, VALUE) VALUES ({2}, {"b"})");
        List<string> results = [];
        IAsyncEnumerable<string> queryResults = dataSource.ExecuteCommandAsync($"SELECT * FROM TESTTABLE",
            async row => $"{await row.GetValueAsync<int>(0)}{await row.GetValueAsync<string>(1)}");
        await foreach (string queryResult in queryResults)
        {
            results.Add(queryResult);
        }
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("1a", results[0]);
        Assert.AreEqual("2b", results[1]);
    }

    [TestMethod]
    public void TestReadWriteNull()
    {
        using TempFile tempFile = new();
        SqliteDataSourceFactory dataSourceFactory = new();
        DbDataSource dataSource = dataSourceFactory.CreateDataSource(tempFile.FilePath, SqliteOpenMode.ReadWriteCreate);
        dataSource.ExecuteCommand($"CREATE TABLE TESTTABLE (ID INT PRIMARY KEY NOT NULL, VALUE TEXT);");
        dataSource.ExecuteCommand($"INSERT INTO TESTTABLE (ID, VALUE) VALUES ({1}, {null})");
        bool isNull = dataSource
            .ExecuteCommand($"SELECT * FROM TESTTABLE", row => row.IsDBNull(1))
            .First();
        Assert.IsTrue(isNull);
    }

    [TestMethod]
    public async Task TestReadWriteNullAsync()
    {
        using TempFile tempFile = new();
        SqliteDataSourceFactory dataSourceFactory = new();
        DbDataSource dataSource = dataSourceFactory.CreateDataSource(tempFile.FilePath, SqliteOpenMode.ReadWriteCreate);
        await dataSource.ExecuteCommandAsync($"CREATE TABLE TESTTABLE (ID INT PRIMARY KEY NOT NULL, VALUE TEXT);");
        await dataSource.ExecuteCommandAsync($"INSERT INTO TESTTABLE (ID, VALUE) VALUES ({1}, {null})");
        List<bool> results = [];
        IAsyncEnumerable<bool> queryResults = dataSource
            .ExecuteCommandAsync($"SELECT * FROM TESTTABLE", row => row.IsDBNullAsync(1));
        await foreach (bool queryResult in queryResults)
        {
            results.Add(queryResult);
        }
        Assert.IsTrue(results[0]);
    }
}
