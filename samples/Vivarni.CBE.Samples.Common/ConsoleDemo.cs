using Microsoft.EntityFrameworkCore;
using Vivarni.CBE.Samples.Common.DomainModels;

namespace Vivarni.CBE.Samples.Common;

public class ConsoleDemo
{
    private readonly SearchDbContext _ctx;

    public ConsoleDemo(SearchDbContext ctx)
    {
        _ctx = ctx;
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
            var results = SearchCompanies(input);
            foreach (var item in results)
                DisplayCompany(item);

            Console.WriteLine();
            Console.WriteLine();
        }
    }

    private List<Enterprise> SearchCompanies(string searchTerm)
    {
        var companies = _ctx
            .Set<Enterprise>()
            .Include(s => s.Denominations)
            .Include(s => s.Addresses)
            .Where(s => s.Denominations.Any(d => d.Name.ToLower().Contains(searchTerm)));

        return [.. companies];
    }

    private static void DisplayCompany(Enterprise enterprise)
    {
        Console.WriteLine($"# {enterprise.Denominations.FirstOrDefault()?.Name}");
        Console.WriteLine($"  Enterprise #: {enterprise.EnterpriseNumber}");
        Console.WriteLine($"  Legal Form  : {enterprise.JuridicalSituation}");

        foreach (var address in enterprise.Addresses)
            Console.WriteLine($"  Address     : {BuildAddress(address)}");
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
