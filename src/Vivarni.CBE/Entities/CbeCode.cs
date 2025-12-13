using System.ComponentModel.DataAnnotations;
using Vivarni.CBE.DataSources;

#pragma warning disable CS8618

namespace Vivarni.CBE.Entities;

[CsvFileMapping("code")]
[CbePrimaryKey(nameof(Code), nameof(Language), nameof(Category))]
public class CbeCode : ICbeEntity
{
    /// <summary>
    /// Indicates which "code table" is involved. The value in category corresponds to the value
    /// specified in the following chapters in the code table column.
    /// </summary>
    [CsvIndex(0), MaxLength(36)] public string Category { get; set; }

    /// <summary>
    /// The code for which a description is given. A code belongs to a certain category. The format
    /// depends on the category to which the code belongs. For example: for 'JuridicalSituation' the
    /// format is 'XXX' (text, 3 positions). The format used can be found in the following chapters in
    /// the description of the variables where this code is used.
    /// </summary>
    [CsvIndex(1), MaxLength(14)] public string Code { get; set; }

    /// <summary>
    /// The language in which the following description is expressed. All codes have a description in
    /// Dutch and French. Some codes also have a description in German and/or English.
    /// </summary>
    [CsvIndex(2), MaxLength(2)] public string Language { get; set; }

    /// <summary>
    /// The description of the given code - belonging to the given category - in the given language.
    /// </summary>
    [CsvIndex(3)] public string Description { get; set; }
}

#pragma warning restore CS8618
