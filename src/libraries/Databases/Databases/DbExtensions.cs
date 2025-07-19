using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Databases;

public static class DbExtensions
{
    public static void Initialize(this DbCommand command, FormattableString text)
    {
        command.CommandText = string.Format(
            CultureInfo.InvariantCulture,
            text.Format,
            Enumerable.Range(0, text.ArgumentCount).Select(i => $"@p{i}").ToArray());
        command.Parameters.Clear();
        for (int i = 0; i < text.ArgumentCount; i++)
        {
            DbParameter parameter = command.CreateParameter();
            parameter.ParameterName = $"@p{i}";
            parameter.Value = text.GetArgument(i) ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }

    public static void ExecuteCommand(this DbDataSource dataSource, FormattableString commandText)
    {
        using DbConnection connection = dataSource.OpenConnection();
        using DbCommand command = connection.CreateCommand();
        command.Initialize(commandText);
        command.ExecuteNonQuery();
    }

    public static IEnumerable<T> ExecuteCommand<T>(this DbDataSource dataSource, FormattableString commandText, Func<IDatabaseRow, T> mapper)
    {
        using DbConnection connection = dataSource.OpenConnection();
        using DbCommand command = connection.CreateCommand();
        command.Initialize(commandText);
        using DbDataReader dataReader = command.ExecuteReader();
        IDatabaseRow row = new DatabaseRow(dataReader);
        while (dataReader.Read())
        {
            yield return mapper(row);
        }
    }

    public static async Task ExecuteCommandAsync(this DbDataSource dataSource, FormattableString commandText, CancellationToken cancellationToken = default)
    {
        DbConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredConnection = connection.ConfigureAwait(false);
        DbCommand command = connection.CreateCommand();
        await using ConfiguredAsyncDisposable configuredCommand = command.ConfigureAwait(false);
        command.Initialize(commandText);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async IAsyncEnumerable<T> ExecuteCommandAsync<T>(this DbDataSource dataSource, FormattableString commandText, Func<IAsyncDatabaseRow, Task<T>> mapper, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        DbConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredConnection = connection.ConfigureAwait(false);
        DbCommand command = connection.CreateCommand();
        await using ConfiguredAsyncDisposable configuredCommand = command.ConfigureAwait(false);
        command.Initialize(commandText);
        DbDataReader dataReader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredDataReader = dataReader.ConfigureAwait(false);
        IAsyncDatabaseRow row = new AsyncDatabaseRow(dataReader, cancellationToken);
        while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            yield return await mapper(row).ConfigureAwait(false);
        }
    }
}
