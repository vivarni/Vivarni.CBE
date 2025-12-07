using System.ComponentModel.DataAnnotations;

namespace Vivarni.CBE.DataSources.Entities;

[CsvFileMapping("code")]
public class CbeCode : ICbeEntity
{
    [CsvIndex(0), MaxLength(36)] public string Category { get; set; }
    [CsvIndex(1), MaxLength(14)] public string Code { get; set; }
    [CsvIndex(2), MaxLength(2)] public string Language { get; set; }
    [CsvIndex(3)] public string Description { get; set; }
}
