namespace Vivarni.CBE.Samples.ElasticSearch.DomainModels;

public class Branch
{
    /// <summary>
    /// The id can be used to identify a branch office.
    /// This identifier is not an official number! It cannot be used for any search in another public CBE search.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The start date of the branch office.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// The enterprise number of the entity associated with the branch office.
    /// </summary>
    public string EnterpriseNumber { get; set; }

    public ICollection<Denomination> Denominations { get; set; }
}
