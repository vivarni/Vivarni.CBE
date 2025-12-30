namespace Vivarni.CBE.Samples.Common.DomainModels;

public class Branch
{
    public string Id { get; set; }

    /// <summary>
    /// The start date of the branch office.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// The enterprise number of the entity associated with the branch office.
    /// </summary>
    public string EnterpriseNumber { get; set; }

    public ICollection<Address> Addresses { get; set; }
    public ICollection<Denomination> Denominations { get; set; }
}
