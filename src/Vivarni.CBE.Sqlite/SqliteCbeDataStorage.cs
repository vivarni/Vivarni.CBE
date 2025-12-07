using System.Data;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Vivarni.CBE.DataSources;
using Vivarni.CBE.DataStorage;

namespace Vivarni.CBE.Sqlite
{
    internal class SqliteCbeDataStorage : ICbeDataStorage
    {
        private readonly string _connectionString;

        public SqliteCbeDataStorage(string connectionString)
        {
            _connectionString = connectionString
                ?? throw new InvalidOperationException("Missing connection string for CBE database");
        }

        public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            where T : ICbeEntity
        {
            throw new NotImplementedException();
        }

        public Task ClearAsync<T>(CancellationToken cancellationToken = default)
            where T : ICbeEntity
        {
            var descriptor = typeof(T).Name;
            using var conn = new SqliteConnection(_connectionString);
            using var command = conn.CreateCommand();

            conn.Open();
            command.CommandText = $"DELETE FROM {descriptor}";
            command.CommandType = CommandType.Text;
            command.ExecuteNonQuery();

            return Task.CompletedTask;
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<int> RemoveAsync<T>(IEnumerable<object> entityIds, PropertyInfo deleteOnProperty, CancellationToken cancellationToken = default)
            where T : ICbeEntity
        {
            using var conn = new SqliteConnection(_connectionString);
            using var command = conn.CreateCommand();

            var ids = entityIds.ToArray();
            var tableName = QuoteIdentifier(typeof(T).Name);
            var columnName = QuoteIdentifier(deleteOnProperty.Name);

            await conn.OpenAsync(cancellationToken);

            var paramNames = new string[ids.Length];
            for (var i = 0; i < ids.Length; i++)
            {
                var pName = $"@p{i}";
                paramNames[i] = pName;
                var p = command.CreateParameter();
                p.ParameterName = pName;
                p.DbType = DbType.Int64;
                p.Value = ids[i];
                command.Parameters.Add(p);
            }

            command.CommandType = CommandType.Text;
            command.CommandText = $"DELETE FROM {tableName} WHERE {columnName} IN ({string.Join(",", paramNames)})";

            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Identifier cannot be null/empty.", nameof(identifier));
            var safe = identifier.Replace("\"", "\"\"");
            return $"\"{safe}\"";
        }
    }
}
