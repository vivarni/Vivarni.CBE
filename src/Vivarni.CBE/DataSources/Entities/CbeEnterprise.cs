using System.ComponentModel.DataAnnotations;

namespace Vivarni.CBE.DataSources.Entities;

[CsvFileMapping("enterprise")]
public class CbeEnterprise : ICbeEntity
{
    [CsvIndex(0), MaxLength(16), IndexColumn] public string EnterpriseNumber { get; set; }
    [CsvIndex(1)] public string Status { get; set; }
    [CsvIndex(2)] public string JuridicalSituation { get; set; }
    [CsvIndex(3)] public string TypeOfEnterprise { get; set; }
    [CsvIndex(4)] public string? JuridicalForm { get; set; }
    [CsvIndex(5)] public string? JuridicalFormCAC { get; set; }
    [CsvIndex(6)] public DateOnly StartDate { get; set; }
}
