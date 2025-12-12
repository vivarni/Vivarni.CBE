using System.ComponentModel.DataAnnotations;
#pragma warning disable CS8618

namespace Vivarni.CBE.DataSources.Entities;

/// <summary>
/// Represents an establishment unit (business unit) as defined in the KBO Open Data export. An
/// establishment is a geographically distinct unit where at least one activity of the enterprise is carried
/// out.
/// </summary>
[CsvFileMapping("establishment")]
internal class CbeEstablishment : ICbeEntity
{
    /// <summary>
    /// Unique identifier for the establishment (business unit) as registered in the KBO.
    /// </summary>
    [CsvIndex(0), MaxLength(16), IndexColumn]
    public string EstablishmentNumber { get; set; }

    /// <summary>
    /// The date when the establishment was registered or started its activities.
    /// </summary>
    [CsvIndex(1)]
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// The enterprise number (company number) to which this establishment belongs.
    /// </summary>
    [CsvIndex(2), MaxLength(16), IndexColumn]
    public string EnterpriseNumber { get; set; }
}

#pragma warning restore CS8618
