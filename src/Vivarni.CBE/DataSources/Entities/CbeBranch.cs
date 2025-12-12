using System.ComponentModel.DataAnnotations;
#pragma warning disable CS8618

namespace Vivarni.CBE.DataSources.Entities;

[CsvFileMapping("branch")]
public class CbeBranch : ICbeEntity
{
    /// <summary>
    /// The id can be used to identify a branch office.
    /// </summary>
    [CsvIndex(0), MaxLength(16), PrimaryKeyColumn]
    public string Id { get; set; }

    /// <summary>
    /// The start date of the branch office.
    /// </summary>
    [CsvIndex(1)]
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// The enterprise number of the entity associated with the branch office.
    /// </summary>
    [CsvIndex(2), MaxLength(16), IndexColumn]
    public string EnterpriseNumber { get; set; }
}

#pragma warning restore CS8618
