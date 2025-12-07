namespace Vivarni.CBE.DataSources.Mappings;

[AttributeUsage(AttributeTargets.Class)]
public class CsvFileMapping : Attribute
{
    public string CsvBaseName { get; set; }

    public CsvFileMapping(string csvBaseName)
    {
        CsvBaseName = csvBaseName;
    }
}
