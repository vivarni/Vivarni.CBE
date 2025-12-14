using System.ComponentModel.DataAnnotations;
using Vivarni.CBE.DataSources;

#pragma warning disable CS8618

namespace Vivarni.CBE.Entities;

[CsvFileMapping("branch")]
[CbePrimaryKey(nameof(Id))]
public class CbeBranch : ICbeEntity
{
    /// <summary>
    /// The id can be used to identify a branch office.
    /// This identifier is not an official number! It cannot be used for any search in another public CBE search.
    /// </summary>
    [CsvIndex(0), MaxLength(16)]
    public string Id { get; set; }

    /// <summary>
    /// The start date of the branch office.
    /// </summary>
    [CsvIndex(1)]
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// The enterprise number of the entity associated with the branch office.
    /// </summary>
    [CsvIndex(2), MaxLength(16), CbeIndex]
    public string EnterpriseNumber { get; set; }
}

#pragma warning restore CS8618
