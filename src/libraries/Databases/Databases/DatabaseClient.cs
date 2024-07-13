using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Databases;

public class DatabaseClient : IDatabaseClient
{
    private readonly DbProviderFactory _providerFactory;

    public string? ConnectionString { get; set; }

    public DatabaseClient(DbProviderFactory providerFactory)
    {
        _providerFactory = providerFactory;
    }

    public async Task ExecuteNonQueryAsync(FormattableString commandText)
    {
        await using DbConnection connection = PrepareConnection(_providerFactory, ConnectionString);
        await connection.OpenAsync();
        await using DbCommand command = PrepareCommand(connection, commandText);
        await command.ExecuteNonQueryAsync();
    }

    public void ExecuteNonQuery(FormattableString commandText)
    {
        using DbConnection connection = PrepareConnection(_providerFactory, ConnectionString);
        connection.Open();
        using DbCommand command = PrepareCommand(connection, commandText);
        command.ExecuteNonQuery();
    }

    public async IAsyncEnumerable<T> ExecuteQueryAsync<T>(FormattableString commandText, Func<IDatabaseRow, Task<T>> mapper)
    {
        await using DbConnection connection = PrepareConnection(_providerFactory, ConnectionString);
        await connection.OpenAsync();
        await using DbCommand command = PrepareCommand(connection, commandText);
        await using DbDataReader dataReader = await command.ExecuteReaderAsync();
        IDatabaseRow row = new DatabaseRow(dataReader);
        while (await dataReader.ReadAsync())
        {
            yield return await mapper(row);
        }
    }

    public IEnumerable<T> ExecuteQuery<T>(FormattableString commandText, Func<IDatabaseRow, T> mapper)
    {
        using DbConnection connection = PrepareConnection(_providerFactory, ConnectionString);
        connection.Open();
        using DbCommand command = PrepareCommand(connection, commandText);
        DbDataReader dataReader = command.ExecuteReader();
        IDatabaseRow row = new DatabaseRow(dataReader);
        while (dataReader.Read())
        {
            yield return mapper(row);
        }
    }

    private static DbConnection PrepareConnection(DbProviderFactory providerFactory, string? connectionString)
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
            parameter.Value = commandText.GetArgument(i) ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
        return command;
    }
}
