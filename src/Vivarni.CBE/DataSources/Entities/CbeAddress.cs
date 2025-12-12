using System.ComponentModel.DataAnnotations;

namespace Vivarni.CBE.DataSources.Entities;

[CsvFileMapping("address")]
public class CbeAddress : ICbeEntity
{
    [CsvIndex(0), MaxLength(16), IndexColumn]
    public string EntityNumber { get; set; }

    [CsvIndex(1)]
    public string TypeOfAddress { get; set; }

    [CsvIndex(2)]
    public string? CountryNL { get; set; }

    [CsvIndex(3)]
    public string? CountryFR { get; set; }

    [CsvIndex(4)]
    public string? Zipcode { get; set; }

    [CsvIndex(5)]
    public string? MunicipalityNL { get; set; }

    [CsvIndex(6)]
    public string? MunicipalityFR { get; set; }

    [CsvIndex(7)]
    public string? StreetNL { get; set; }

    [CsvIndex(8)]
    public string? StreetFR { get; set; }

    [CsvIndex(9)]
    public string? HouseNumber { get; set; }

    [CsvIndex(10)]
    public string? Box { get; set; }

    [CsvIndex(11)]
    public string? ExtraAddressInfo { get; set; }

    [CsvIndex(12)]
    public DateOnly? DateStrikingOff { get; set; }
}
