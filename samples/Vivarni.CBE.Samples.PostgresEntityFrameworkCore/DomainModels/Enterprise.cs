namespace Vivarni.CBE.Samples.PostgresEntityFrameworkCore.DomainModels;

/// <summary>
/// Represents an enterprise (legal entity or natural person) as defined in the KBO Open Data export.
/// Contains core information about the company or self-employed person registered in the Belgian
/// Crossroads Bank for Enterprises (KBO).
/// </summary>
public class Enterprise
{
    /// <summary>
    /// Unique identifier for the enterprise (company number) as registered in the KBO.
    /// </summary>
    public string EnterpriseNumber { get; set; }

    /// <summary>
    /// The current status of the enterprise (e.g., 'Active', 'Struck off', etc.).
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// The legal situation of the enterprise (e.g., 'In liquidation', 'Bankrupt', etc.).
    /// </summary>
    public string JuridicalSituation { get; set; }

    /// <summary>
    /// The type of enterprise (e.g., 'Legal entity', 'Natural person').
    /// </summary>
    public char TypeOfEnterprise { get; set; }

    /// <summary>
    /// The legal form of the entity, if it is a legal entity. See code table. (Examples: 'NV', 'BVBA', etc.)
    /// </summary>
    public string? JuridicalForm { get; set; }

    /// <summary>
    /// Contains the legal form as it should be interpreted, pending the adjustment of the statutes in
    /// accordance with the Belgian Companies and Associations Code (WVV).
    /// </summary>
    public string? JuridicalFormCAC { get; set; }

    /// <summary>
    /// The start date of the entity. For legal entities, this is the start date of the first legal status with
    /// status 'published' or 'active'. For natural persons, this is the start date of the last period in which
    /// the entity is in status 'published' or 'active'.
    /// </summary>
    public DateOnly StartDate { get; set; }

    public ICollection<Contact> Contacts { get; set; }
    public ICollection<Address> Addresses { get; set; }
    public ICollection<Activity> Activities { get; set; }
    public ICollection<Branch> Branches { get; set; }
    public ICollection<Establishment> Establishments { get; set; }
    public ICollection<Denomination> Denominations { get; set; }
}
