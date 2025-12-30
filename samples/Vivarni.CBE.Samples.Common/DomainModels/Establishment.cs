namespace Vivarni.CBE.Samples.Common.DomainModels;

/// <summary>
/// Represents an establishment unit (business unit) as defined in the KBO Open Data export. An
/// establishment is a geographically distinct unit where at least one activity of the enterprise is carried
/// out.
/// </summary>
public class Establishment
{
    /// <summary>
    /// Unique identifier for the establishment (business unit) as registered in the KBO.
    /// </summary>
    public string EstablishmentNumber { get; set; }

    /// <summary>
    /// The date when the establishment was registered or started its activities.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// The enterprise number (company number) to which this establishment belongs.
    /// </summary>
    public string EnterpriseNumber { get; set; }

    public ICollection<Contact> Contacts { get; set; }
    public ICollection<Address> Addresses { get; set; }
    public ICollection<Activity> Activities { get; set; }
    public ICollection<Denomination> Denominations { get; set; }
}
