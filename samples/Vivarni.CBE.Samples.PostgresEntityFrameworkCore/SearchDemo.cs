using Microsoft.EntityFrameworkCore;
using Vivarni.CBE.Samples.PostgresEntityFrameworkCore.DomainModels;

namespace Vivarni.CBE.Samples.PostgresEntityFrameworkCore;

internal class SearchDemo
{
    private readonly SearchDbContext _ctx;

    public SearchDemo(SearchDbContext ctx)
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
            .Where(s => s.Denominations.Any(d => d.Name.ToLower().Contains(searchTerm)));

        return [.. companies];
    }

    private static void DisplayCompany(Enterprise enterprise)
    {
        Console.WriteLine($"# {enterprise.Denominations.FirstOrDefault()?.Name}");
        Console.WriteLine($"  Enterprise #: {enterprise.EnterpriseNumber}");
        Console.WriteLine($"  Legal Form:   {enterprise.JuridicalSituation}");

        //if (!string.IsNullOrEmpty(address))
        //    Console.WriteLine($"   Address:      {address}");
    }

    //private static string BuildAddress(CompanyResult company)
    //{
    //    var parts = new List<string>();
    //
    //    if (!string.IsNullOrEmpty(company.Street))
    //    {
    //        var streetPart = company.Street;
    //        if (!string.IsNullOrEmpty(company.HouseNumber))
    //            streetPart += $" {company.HouseNumber}";
    //        parts.Add(streetPart);
    //    }
    //
    //    if (!string.IsNullOrEmpty(company.Zipcode) || !string.IsNullOrEmpty(company.City))
    //    {
    //        var cityPart = "";
    //        if (!string.IsNullOrEmpty(company.Zipcode))
    //            cityPart = company.Zipcode;
    //        if (!string.IsNullOrEmpty(company.City))
    //            cityPart += string.IsNullOrEmpty(cityPart) ? company.City : $" {company.City}";
    //
    //        if (!string.IsNullOrEmpty(cityPart))
    //            parts.Add(cityPart);
    //    }
    //
    //    return string.Join(", ", parts);
    //}
}
