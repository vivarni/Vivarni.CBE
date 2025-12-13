using System.ComponentModel.DataAnnotations;
using Vivarni.CBE.DataSources;

#pragma warning disable CS8618

namespace Vivarni.CBE.Entities;

[CsvFileMapping("address")]
[CbePrimaryKey(nameof(EntityNumber), nameof(TypeOfAddress))]
public class CbeAddress : ICbeEntity
{
    /// <summary>
    /// The establishment unit or enterprise number.
    /// </summary>
    [CsvIndex(0), MaxLength(16), CbeIndex]
    public string EntityNumber { get; set; }

    /// <summary>
    /// Type of address.
    /// </summary>
    [CsvIndex(1), MaxLength(4)]
    public string TypeOfAddress { get; set; }

    /// <summary>
    /// Country (Dutch).
    /// </summary>
    [CsvIndex(2), MaxLength(100)]
    public string? CountryNL { get; set; }

    /// <summary>
    /// Country (French).
    /// </summary>
    [CsvIndex(3), MaxLength(100)]
    public string? CountryFR { get; set; }

    /// <summary>
    /// Zip code.
    /// </summary>
    [CsvIndex(4), MaxLength(20)]
    public string? Zipcode { get; set; }

    /// <summary>
    /// Municipality (Dutch).
    /// </summary>
    [CsvIndex(5), MaxLength(200)]
    public string? MunicipalityNL { get; set; }

    /// <summary>
    /// Municipality (French).
    /// </summary>
    [CsvIndex(6), MaxLength(200)]
    public string? MunicipalityFR { get; set; }

    /// <summary>
    /// Street (Dutch).
    /// </summary>
    [CsvIndex(7), MaxLength(200)]
    public string? StreetNL { get; set; }

    /// <summary>
    /// Street (French).
    /// </summary>
    [CsvIndex(8), MaxLength(200)]
    public string? StreetFR { get; set; }

    /// <summary>
    /// House number.
    /// </summary>
    [CsvIndex(9), MaxLength(22)]
    public string? HouseNumber { get; set; }

    /// <summary>
    /// Box.
    /// </summary>
    [CsvIndex(10), MaxLength(20)]
    public string? Box { get; set; }

    /// <summary>
    /// Extra address information.
    /// </summary>
    [CsvIndex(11), MaxLength(80)]
    public string? ExtraAddressInfo { get; set; }

    /// <summary>
    /// Date of striking off.
    /// </summary>
    [CsvIndex(12)]
    public DateOnly? DateStrikingOff { get; set; }
}

#pragma warning restore CS8618
