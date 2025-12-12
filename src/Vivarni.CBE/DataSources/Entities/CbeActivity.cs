using System.ComponentModel.DataAnnotations;
#pragma warning disable CS8618

namespace Vivarni.CBE.DataSources.Entities;

[CsvFileMapping("activity")]
public class CbeActivity : ICbeEntity
{
    /// <summary>
    /// The establishment unit or enterprise number.
    /// </summary>
    [CsvIndex(0), MaxLength(16), IndexColumn]
    public string EntityNumber { get; set; }

    /// <summary>
    /// Type of activity. See code table.
    /// </summary>
    [CsvIndex(1), MaxLength(6)]
    public string ActivityGroup { get; set; }

    /// <summary>
    /// Indicates whether the activity is coded in Nace version 2003, Nace version 2008, or Nace version 2025.
    /// Possible values: 2003, 2008, 2025
    /// </summary>
    [CsvIndex(2), MaxLength(4)]
    public string NaceVersion { get; set; }

    /// <summary>
    /// The activity of the entity or establishment unit, coded in a Nace code (in the indicated version).
    /// See code table (Nace2003, Nace2008, Nace2025).
    /// </summary>
    [CsvIndex(3), MaxLength(14)]
    public string NaceCode { get; set; }

    /// <summary>
    /// Indicates whether this is a main, secondary, or auxiliary activity. See code table.
    /// </summary>
    [CsvIndex(4), MaxLength(4)]
    public string Classification { get; set; }
}

#pragma warning restore CS8618
