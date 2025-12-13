using System.ComponentModel.DataAnnotations;
using Vivarni.CBE.DataSources;

#pragma warning disable CS8618

namespace Vivarni.CBE.Entities;

/// <summary>
/// Represents an enterprise (legal entity or natural person) as defined in the KBO Open Data export.
/// Contains core information about the company or self-employed person registered in the Belgian
/// Crossroads Bank for Enterprises (KBO).
/// </summary>
[CsvFileMapping("enterprise")]
[CbePrimaryKey(nameof(EnterpriseNumber))]
public class CbeEnterprise : ICbeEntity
{
    /// <summary>
    /// Unique identifier for the enterprise (company number) as registered in the KBO.
    /// </summary>
    [CsvIndex(0), MaxLength(12), CbeIndex]
    public string EnterpriseNumber { get; set; }

    /// <summary>
    /// The current status of the enterprise (e.g., 'Active', 'Struck off', etc.).
    /// </summary>
    [CsvIndex(1), MaxLength(2)]
    public string Status { get; set; }

    /// <summary>
    /// The legal situation of the enterprise (e.g., 'In liquidation', 'Bankrupt', etc.).
    /// </summary>
    [CsvIndex(2), MaxLength(3)]
    public string JuridicalSituation { get; set; }

    /// <summary>
    /// The type of enterprise (e.g., 'Legal entity', 'Natural person').
    /// </summary>
    [CsvIndex(3)]
    public char TypeOfEnterprise { get; set; }

    /// <summary>
    /// The legal form of the entity, if it is a legal entity. See code table. (Examples: 'NV', 'BVBA', etc.)
    /// </summary>
    [CsvIndex(4), MaxLength(3)]
    public string? JuridicalForm { get; set; }

    /// <summary>
    /// Contains the legal form as it should be interpreted, pending the adjustment of the statutes in
    /// accordance with the Belgian Companies and Associations Code (WVV).
    /// </summary>
    [CsvIndex(5), MaxLength(3)]
    public string? JuridicalFormCAC { get; set; }

    /// <summary>
    /// The start date of the entity. For legal entities, this is the start date of the first legal status with
    /// status 'published' or 'active'. For natural persons, this is the start date of the last period in which
    /// the entity is in status 'published' or 'active'.
    /// </summary>
    [CsvIndex(6)]
    public DateOnly StartDate { get; set; }
}

#pragma warning restore CS8618
