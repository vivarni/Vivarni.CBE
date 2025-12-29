using Microsoft.EntityFrameworkCore;
using Vivarni.CBE.Samples.Common.DomainModels;

namespace Vivarni.CBE.Samples.Web;

public class ElasticDoc
{
    public string EnterpriseNumber { get; set; }

    /// <summary>
    /// The type of enterprise (e.g., 'Legal entity', 'Natural person').
    /// </summary>
    public char Type { get; set; }

    /// <summary>
    /// The legal form of the entity, if it is a legal entity. See code table. (Examples: 'NV', 'BVBA', etc.)
    /// </summary>
    public string? JuridicalForm { get; set; }

    public IEnumerable<string> Denominations { get; set; }
    public List<string> Addresses { get; set; }

    public ElasticDoc() { }
    public ElasticDoc(Enterprise enterprise)
    {
        var addresses = enterprise.Addresses.Select(s =>
            ((string.IsNullOrEmpty(s.CountryNL) ? "België" : s.CountryNL) + "\n" +
             $"{s.Zipcode} {s.MunicipalityNL}\n" +
             $"{s.StreetNL} {s.HouseNumber} {s.Box}\n" +
             $"{s.ExtraAddressInfo}").Trim());

        EnterpriseNumber = enterprise.EnterpriseNumber;
        Type = enterprise.TypeOfEnterprise;
        JuridicalForm = enterprise.JuridicalForm;
        Denominations = enterprise.Denominations.Select(s => s.Name);
        Addresses = [.. addresses];
    }
}
