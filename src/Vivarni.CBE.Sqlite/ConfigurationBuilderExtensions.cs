namespace Vivarni.CBE.Sqlite;

public static class ConfigurationBuilderExtensions
{
    public static VivarniCbeOptions WithSqliteDatabase(this VivarniCbeOptions builder, string connectionString)
    {
        builder.DataStorageFactory = (s) => new SqliteCbeDataStorage(connectionString);
        return builder;
    }
}
