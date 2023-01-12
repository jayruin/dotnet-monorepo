using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Databases;

public sealed class DatabaseClient : IDatabaseClient
{
    public DatabaseProvider DefaultProvider { get; set; } = DatabaseProvider.None;

    public string DefaultConnectionString { get; set; } = string.Empty;

    public DbProviderFactory GetFactory(DatabaseProvider provider)
    {
        return provider switch
        {
            DatabaseProvider.Sqlite => SqliteFactory.Instance,
            DatabaseProvider.Sqlserver => SqlClientFactory.Instance,
            DatabaseProvider.Postgresql => NpgsqlFactory.Instance,
            _ => throw new ArgumentException("No provider specified!", nameof(provider)),
        };
    }

    public async Task ExecuteNonQueryAsync(DatabaseProvider provider, string connectionString,
        FormattableString commandText)
    {
        DbProviderFactory providerFactory = GetFactory(provider);
        await using DbConnection connection = PrepareConnection(providerFactory, connectionString);
        await connection.OpenAsync();
        await using DbCommand command = PrepareCommand(connection, commandText);
        await command.ExecuteNonQueryAsync();
    }

    public void ExecuteNonQuery(DatabaseProvider provider, string connectionString,
        FormattableString commandText)
    {
        DbProviderFactory providerFactory = GetFactory(provider);
        using DbConnection connection = PrepareConnection(providerFactory, connectionString);
        connection.Open();
        using DbCommand command = PrepareCommand(connection, commandText);
        command.ExecuteNonQuery();
    }

    public async IAsyncEnumerable<T> ExecuteQueryAsync<T>(DatabaseProvider provider, string connectionString,
        FormattableString commandText, Func<IDatabaseRow, Task<T>> mapper)
    {
        DbProviderFactory providerFactory = GetFactory(provider);
        await using DbConnection connection = PrepareConnection(providerFactory, connectionString);
        await connection.OpenAsync();
        await using DbCommand command = PrepareCommand(connection, commandText);
        await using DbDataReader dataReader = await command.ExecuteReaderAsync();
        IDatabaseRow row = new DatabaseRow(dataReader);
        while (await dataReader.ReadAsync())
        {
            yield return await mapper(row);
        }
    }

    public IEnumerable<T> ExecuteQuery<T>(DatabaseProvider provider, string connectionString,
        FormattableString commandText, Func<IDatabaseRow, T> mapper)
    {
        DbProviderFactory providerFactory = GetFactory(provider);
        using DbConnection connection = PrepareConnection(providerFactory, connectionString);
        connection.Open();
        using DbCommand command = PrepareCommand(connection, commandText);
        DbDataReader dataReader = command.ExecuteReader();
        IDatabaseRow row = new DatabaseRow(dataReader);
        while (dataReader.Read())
        {
            yield return mapper(row);
        }
    }

    private static DbConnection PrepareConnection(DbProviderFactory providerFactory, string connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));
        DbConnection connection = providerFactory.CreateConnection()
            ?? throw new InvalidOperationException("Could not create connection");
        connection.ConnectionString = connectionString;
        return connection;
    }

    private static DbCommand PrepareCommand(DbConnection connection, FormattableString commandText)
    {
        DbCommand command = connection.CreateCommand();
        command.CommandText = string.Format(
            CultureInfo.InvariantCulture,
            commandText.Format,
            Enumerable.Range(0, commandText.ArgumentCount).Select(i => $"@p{i}").ToArray());
        for (int i = 0; i < commandText.ArgumentCount; i++)
        {
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = $"@p{i}";
            parameter.Value = commandText.GetArgument(i);
            command.Parameters.Add(parameter);
        }
        return command;
    }

    public Task ExecuteNonQueryAsync(FormattableString commandText)
        => ExecuteNonQueryAsync(DefaultProvider, DefaultConnectionString, commandText);

    public void ExecuteNonQuery(FormattableString commandText)
        => ExecuteNonQuery(DefaultProvider, DefaultConnectionString, commandText);

    public IAsyncEnumerable<T> ExecuteQueryAsync<T>(FormattableString commandText, Func<IDatabaseRow, Task<T>> mapper)
        => ExecuteQueryAsync<T>(DefaultProvider, DefaultConnectionString, commandText, mapper);

    public IEnumerable<T> ExecuteQuery<T>(FormattableString commandText, Func<IDatabaseRow, T> mapper)
        => ExecuteQuery<T>(DefaultProvider, DefaultConnectionString, commandText, mapper);
}
