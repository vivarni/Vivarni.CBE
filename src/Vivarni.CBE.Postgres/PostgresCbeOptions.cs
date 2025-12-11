namespace Vivarni.CBE.Postgres;

public class PostgresCbeOptions
{
    public string Schema { get; set; } = "public";
    public string TablePrefix { get; set; } = string.Empty;
    public int BinaryImporterBatchSize { get; set; } = 500_000;
}
