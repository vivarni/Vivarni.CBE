namespace Vivarni.CBE.Samples.ElasticSearch.DomainModels;

public class Contact
{
    public long Id { get; set; }

    /// <summary>
    /// The establishment unit or enterprise number.
    /// </summary>
    public string EntityNumber { get; set; }

    /// <summary>
    /// Indicates for which type of entity this is a contact detail: enterprise, branch, or establishment
    /// unit. See code table.
    /// </summary>
    public string EntityContact { get; set; }

    /// <summary>
    /// Indicates the type of contact detail: phone number, email, or web address. See code table.
    /// </summary>
    public string ContactType { get; set; }

    /// <summary>
    /// The contact detail: phone number, email, or web address.
    /// </summary>
    public string Value { get; set; }
}
