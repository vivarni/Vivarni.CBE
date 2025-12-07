using System.ComponentModel.DataAnnotations;

namespace Vivarni.CBE.DataSources.Entities;

[CsvFileMapping("denomination")]
public class CbeDenomination : ICbeEntity
{
    [CsvIndex(0), MaxLength(16), IndexColumn] public string EntityNumber { get; set; }
    /// <summary>
    /// Foreign key to the [code] table WHERE category = 'Language'.
    /// </summary>
    [CsvIndex(1)] public byte Language { get; set; }
    [CsvIndex(2)] public string TypeOfDenomination { get; set; }
    [CsvIndex(3)] public string Denomination { get; set; }
}

