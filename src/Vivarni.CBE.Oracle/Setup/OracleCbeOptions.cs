namespace Vivarni.CBE.Oracle.Setup;

public class OracleCbeOptions
{
    public string TablePrefix { get; set; } = string.Empty;
    public int BulkInsertBatchSize { get; set; } = 100_000;
}
