using System.ComponentModel.DataAnnotations;

namespace Vivarni.CBE.DataSources.Entities;

[CsvFileMapping("branch")]
public class CbeBranch : ICbeEntity
{
    [CsvIndex(0), MaxLength(16), PrimaryKeyColumn]
    public string Id { get; set; }

    [CsvIndex(1)]
    public DateOnly StartDate { get; set; }

    [CsvIndex(2), MaxLength(16), IndexColumn]
    public string EnterpriseNumber { get; set; }
}
