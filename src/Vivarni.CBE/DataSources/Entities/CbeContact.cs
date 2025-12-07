using System.ComponentModel.DataAnnotations;

namespace Vivarni.CBE.DataSources.Entities;

[CsvFileMapping("contact")]
public class CbeContact : ICbeEntity
{
    [CsvIndex(0), MaxLength(16), IndexColumn] public string EntityNumber { get; set; }
    [CsvIndex(1), MaxLength(5)] public string EntityContact { get; set; }
    [CsvIndex(2), MaxLength(10)] public string ContactType { get; set; }
    [CsvIndex(3)] public string Value { get; set; }
}
