using System.ComponentModel.DataAnnotations;
#pragma warning disable CS8618

namespace Vivarni.CBE.DataSources.Entities;

/// <summary>
/// Represents a metadata record from the KBO Open Data export. Contains general information about the
/// dataset, such as version, creation date, and other metadata variables.
/// </summary>
[CsvFileMapping("meta")]
internal class CbeMeta : ICbeEntity
{
    /// <summary>
    /// The name of the metadata variable (e.g., 'Version', 'CreationDate', etc.).
    /// </summary>
    [CsvIndex(0), MaxLength(20)]
    public string Variable { get; set; }

    /// <summary>
    /// The value associated with the metadata variable.
    /// </summary>
    [CsvIndex(1), MaxLength(1000)]
    public string? Value { get; set; }
}

#pragma warning restore CS8618
