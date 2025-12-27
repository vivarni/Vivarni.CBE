namespace Vivarni.CBE.Samples.ElasticSearch.DomainModels;

public class Denomination
{
    public long Id { get; set; }

    /// <summary>
    /// The establishment unit or enterprise number.
    /// </summary>
    public string EntityNumber { get; set; }

    /// <summary>
    /// Language of the name. See code table (category=language).
    /// </summary>
    public byte Language { get; set; }

    /// <summary>
    /// Type of name. See code table.
    /// </summary>
    public string TypeOfDenomination { get; set; }

    /// <summary>
    /// The name of the entity, branch, or establishment unit.
    /// </summary>
    public string Name { get; set; }
}

