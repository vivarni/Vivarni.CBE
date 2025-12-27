namespace Vivarni.CBE.Samples.ElasticSearch.DomainModels;

public class Address
{
    public long Id { get; set; }

    /// <summary>
    /// The establishment unit or enterprise number.
    /// </summary>
    public string EntityNumber { get; set; }

    /// <summary>
    /// Type of address.
    /// </summary>
    public string TypeOfAddress { get; set; }

    /// <summary>
    /// Country (Dutch).
    /// </summary>
    public string? CountryNL { get; set; }

    /// <summary>
    /// Country (French).
    /// </summary>
    public string? CountryFR { get; set; }

    /// <summary>
    /// Zip code.
    /// </summary>
    public string? Zipcode { get; set; }

    /// <summary>
    /// Municipality (Dutch).
    /// </summary>
    public string? MunicipalityNL { get; set; }

    /// <summary>
    /// Municipality (French).
    /// </summary>
    public string? MunicipalityFR { get; set; }

    /// <summary>
    /// Street (Dutch).
    /// </summary>
    public string? StreetNL { get; set; }

    /// <summary>
    /// Street (French).
    /// </summary>
    public string? StreetFR { get; set; }

    /// <summary>
    /// House number.
    /// </summary>
    public string? HouseNumber { get; set; }

    /// <summary>
    /// Box.
    /// </summary>
    public string? Box { get; set; }

    /// <summary>
    /// Extra address information.
    /// </summary>
    public string? ExtraAddressInfo { get; set; }

    /// <summary>
    /// Date of striking off.
    /// </summary>
    public DateOnly? DateStrikingOff { get; set; }
}
