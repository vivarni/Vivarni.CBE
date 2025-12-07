namespace Vivarni.CBE.DataStorage;

public class TableDescriptor
{
    public string TableName { get; set; }

    public TableDescriptor(string tableName)
    {
        TableName = tableName;
    }
}
