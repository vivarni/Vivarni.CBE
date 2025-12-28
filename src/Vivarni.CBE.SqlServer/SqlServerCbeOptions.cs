namespace Vivarni.CBE.SqlServer
{
    public class SqlServerCbeOptions
    {
        public int BulkCopyBatchSize { get; set; } = 500_000;
        public string Schema { get; set; } = "dbo";
        public string TablePrefix { get; set; } = string.Empty;
    }
}
