using System.ComponentModel.DataAnnotations;

namespace Vivarni.CBE.DataSources.Entities;

[CsvFileMapping("establishment")]
internal class CbeEstablishment : ICbeEntity
{
    [CsvIndex(0), MaxLength(16), IndexColumn] public string EstablishmentNumber { get; set; }
    [CsvIndex(1)] public DateOnly StartDate { get; set; }
    [CsvIndex(2), MaxLength(16), IndexColumn] public string EnterpriseNumber { get; set; }
}
