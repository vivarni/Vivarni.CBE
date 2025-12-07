using System.ComponentModel.DataAnnotations;

namespace Vivarni.CBE.DataSources.Entities;

[CsvFileMapping("meta")]
internal class CbeMeta : ICbeEntity
{
    [CsvIndex(0), MaxLength(20)] public string Variable { get; set; }
    [CsvIndex(1), MaxLength(1000)] public string Value { get; set; }
}
