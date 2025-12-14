using System.ComponentModel.DataAnnotations;
using Vivarni.CBE.DataSources;

#pragma warning disable CS8618

namespace Vivarni.CBE.Entities;

[CsvFileMapping("contact")]
[CbePrimaryKey(nameof(EntityNumber), nameof(EntityContact), nameof(ContactType), nameof(Value))]
public class CbeContact : ICbeEntity
{
    /// <summary>
    /// The establishment unit or enterprise number.
    /// </summary>
    [CsvIndex(0), MaxLength(16)] public string EntityNumber { get; set; }

    /// <summary>
    /// Indicates for which type of entity this is a contact detail: enterprise, branch, or establishment
    /// unit. See code table.
    /// </summary>
    [CsvIndex(1), MaxLength(3)] public string EntityContact { get; set; }

    /// <summary>
    /// Indicates the type of contact detail: phone number, email, or web address. See code table.
    /// </summary>
    [CsvIndex(2), MaxLength(5)] public string ContactType { get; set; }

    /// <summary>
    /// The contact detail: phone number, email, or web address.
    /// </summary>
    [CsvIndex(3), MaxLength(254)] public string Value { get; set; }
}

#pragma warning restore CS8618
