using System.ComponentModel.DataAnnotations;

namespace Vivarni.CBE.DataSources.Entities;

[CsvFileMapping("activity")]
internal class CbeActivity : ICbeEntity
{
    [CsvIndex(0), MaxLength(16), IndexColumn]
    public string EntityNumber { get; set; }

    [CsvIndex(1), MaxLength(6)]
    public string ActivityGroup { get; set; }

    /// <summary>
    /// Can be one of the following values: 2003, 2008, 2025
    /// </summary>
    [CsvIndex(2), MaxLength(4)]
    public string NaceVersion { get; set; }

    [CsvIndex(3), MaxLength(14)]
    public string NaceCode { get; set; }

    [CsvIndex(4), MaxLength(4)]
    public string Classification { get; set; }
}
