using System.ComponentModel.DataAnnotations;

namespace Vivarni.CBE.DataSources.Entities;

[CsvFileMapping("address")]
public class CbeAddress : ICbeEntity
{
    [CsvIndex(0), MaxLength(16), IndexColumn]
    public string EntityNumber { get; set; }

    [CsvIndex(1), MaxLength(4)]
    public string TypeOfAddress { get; set; }

    [CsvIndex(2), MaxLength(100)]
    public string? CountryNL { get; set; }

    [CsvIndex(3), MaxLength(100)]
    public string? CountryFR { get; set; }

    [CsvIndex(4), MaxLength(20)]
    public string? Zipcode { get; set; }

    [CsvIndex(5), MaxLength(200)]
    public string? MunicipalityNL { get; set; }

    [CsvIndex(6), MaxLength(200)]
    public string? MunicipalityFR { get; set; }

    [CsvIndex(7), MaxLength(200)]
    public string? StreetNL { get; set; }

    [CsvIndex(8), MaxLength(200)]
    public string? StreetFR { get; set; }

    [CsvIndex(9), MaxLength(22)]
    public string? HouseNumber { get; set; }

    [CsvIndex(10), MaxLength(20)]
    public string? Box { get; set; }

    [CsvIndex(11), MaxLength(80)]
    public string? ExtraAddressInfo { get; set; }

    [CsvIndex(12)]
    public DateOnly? DateStrikingOff { get; set; }
}
