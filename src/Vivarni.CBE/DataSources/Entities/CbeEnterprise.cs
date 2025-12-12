using System.ComponentModel.DataAnnotations;


namespace Vivarni.CBE.DataSources.Entities;


/// <summary>
/// Represents an enterprise (legal entity or natural person) as defined in the KBO Open Data export.
/// Contains core information about the company or self-employed person registered in the Belgian Crossroads Bank for Enterprises (KBO).
/// </summary>
[CsvFileMapping("enterprise")]
public class CbeEnterprise : ICbeEntity
{
    /// <summary>
    /// Unique identifier for the enterprise (company number) as registered in the KBO.
    /// </summary>
    [CsvIndex(0), MaxLength(12), IndexColumn]
    public string EnterpriseNumber { get; set; }

    /// <summary>
    /// The current status of the enterprise (e.g., 'Active', 'Struck off', etc.).
    /// </summary>
    [CsvIndex(1), MaxLength(2)]
    public string Status { get; set; }

    /// <summary>
    /// The juridical situation of the enterprise (e.g., 'In liquidation', 'Bankrupt', etc.).
    /// </summary>
    [CsvIndex(2), MaxLength(3)]
    public string JuridicalSituation { get; set; }

    /// <summary>
    /// The type of enterprise (e.g., 'Legal entity', 'Natural person').
    /// </summary>
    [CsvIndex(3)]
    public char TypeOfEnterprise { get; set; }

    /// <summary>
    /// The legal form of the enterprise (e.g., 'NV', 'BVBA', etc.), if available.
    /// </summary>
    [CsvIndex(4), MaxLength(3)]
    public string? JuridicalForm { get; set; }

    /// <summary>
    /// The legal form code (CAC) of the enterprise, if available.
    /// </summary>
    [CsvIndex(5), MaxLength(3)]
    public string? JuridicalFormCAC { get; set; }

    /// <summary>
    /// The date when the enterprise was registered or started its activities.
    /// </summary>
    [CsvIndex(6)]
    public DateOnly StartDate { get; set; }
}
