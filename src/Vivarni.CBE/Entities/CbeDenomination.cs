using System.ComponentModel.DataAnnotations;
using Vivarni.CBE.DataSources;

#pragma warning disable CS8618

namespace Vivarni.CBE.Entities;

[CsvFileMapping("denomination")]
[CbePrimaryKey(nameof(EntityNumber), nameof(TypeOfDenomination), nameof(Language))]
public class CbeDenomination : ICbeEntity
{
    /// <summary>
    /// The establishment unit or enterprise number.
    /// </summary>
    [CsvIndex(0), MaxLength(16), CbeIndex] public string EntityNumber { get; set; }

    /// <summary>
    /// Language of the name. See code table (category=language).
    /// </summary>
    [CsvIndex(1)] public byte Language { get; set; }

    /// <summary>
    /// Type of name. See code table.
    /// </summary>
    [CsvIndex(2), MaxLength(3)] public string TypeOfDenomination { get; set; }

    /// <summary>
    /// The name of the entity, branch, or establishment unit.
    /// </summary>
    [CsvIndex(3), MaxLength(320)] public string Denomination { get; set; }
}

#pragma warning restore CS8618

