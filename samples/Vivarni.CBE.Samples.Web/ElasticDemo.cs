using Microsoft.EntityFrameworkCore;
using Vivarni.CBE.Samples.Common;
using Vivarni.CBE.Samples.Common.DomainModels;

namespace Vivarni.CBE.Samples.Web;

public class ElasticDemo
{
    private readonly SearchDbContext _ctx;
    private readonly IElasticRepository _repository;

    public ElasticDemo(SearchDbContext ctx, IElasticRepository repository)
    {
        _ctx = ctx;
        _repository = repository;
    }

    public async Task Run()
    {
        while (true)
        {
            Console.WriteLine("────────────────────────────────────────────────────────");
            Console.WriteLine("Enter company name to search (or 'quit' to exit):");
            Console.Write("> ");

            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
                return;

            if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("q", StringComparison.OrdinalIgnoreCase))
                return;

            Console.WriteLine();
            var results = await _repository.SearchAsync(input);
            foreach (var item in results)
                DisplayCompany(item);

            Console.WriteLine();
            Console.WriteLine();
        }
    }

    private static void DisplayCompany(Enterprise enterprise)
    {
        Console.WriteLine($"# {enterprise.Denominations.FirstOrDefault()?.Name}");
        Console.WriteLine($"  Enterprise #: {enterprise.EnterpriseNumber}");
        Console.WriteLine($"  Legal Form  : {enterprise.JuridicalSituation}");

        foreach (var address in enterprise.Addresses)
            Console.WriteLine($"  Address     : {BuildAddress(address)}");

        Console.WriteLine();
    }

    private static string BuildAddress(Address addr)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(addr.StreetNL))
        {
            var streetPart = addr.StreetNL;
            if (!string.IsNullOrEmpty(addr.HouseNumber))
                streetPart += $" {addr.HouseNumber}";
            parts.Add(streetPart);
        }

        if (!string.IsNullOrEmpty(addr.Zipcode) || !string.IsNullOrEmpty(addr.MunicipalityNL))
        {
            var cityPart = "";
            if (!string.IsNullOrEmpty(addr.Zipcode))
                cityPart = addr.Zipcode;
            if (!string.IsNullOrEmpty(addr.MunicipalityNL))
                cityPart += string.IsNullOrEmpty(cityPart) ? addr.MunicipalityNL : $" {addr.MunicipalityNL}";

            if (!string.IsNullOrEmpty(cityPart))
                parts.Add(cityPart);
        }

        return string.Join(", ", parts);
    }
}
