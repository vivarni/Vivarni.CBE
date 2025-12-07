using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace Vivarni.CBE.ConsoleSqlite;

internal class SearchDemo
{
    private readonly string _connectionString;

    public SearchDemo(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("sqlite")!;
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
            var results = SearchCompanies(_connectionString, input);
            foreach (var item in results)
                DisplayCompany(item);

            Console.WriteLine();
            Console.WriteLine();
        }
    }

    private static List<CompanyResult> SearchCompanies(string connectionString, string searchTerm)
    {
        var query = @"
                SELECT e.EnterpriseNumber, 
                       cc.Description as LegalForm, 
                       cd.Denomination as CompanyName,
                       ca.StreetNL as Street,
                       ca.HouseNumber,
                       ca.Zipcode,
                       ca.MunicipalityNL as City
                FROM CbeEnterprise e
                INNER JOIN CbeCode cc ON cc.Code = e.JuridicalForm 
                    AND cc.Category = 'JuridicalForm' 
                    AND cc.""Language"" = 'NL'
                INNER JOIN CbeDenomination cd ON cd.EntityNumber = e.EnterpriseNumber 
                INNER JOIN CbeAddress ca ON ca.EntityNumber = e.EnterpriseNumber 
                WHERE cd.Denomination LIKE @searchTerm
                LIMIT 10";

        using var conn = new SqliteConnection(connectionString);
        return [.. conn.Query<CompanyResult>(query, new { searchTerm = $"%{searchTerm}%" })];
    }

    private static void DisplayCompany(CompanyResult company)
    {
        Console.WriteLine($"# {company.CompanyName}");
        Console.WriteLine($"  Enterprise #: {company.EnterpriseNumber}");
        Console.WriteLine($"  Legal Form:   {company.LegalForm}");

        var address = BuildAddress(company);
        if (!string.IsNullOrEmpty(address))
            Console.WriteLine($"   Address:      {address}");
    }

    private static string BuildAddress(CompanyResult company)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(company.Street))
        {
            var streetPart = company.Street;
            if (!string.IsNullOrEmpty(company.HouseNumber))
                streetPart += $" {company.HouseNumber}";
            parts.Add(streetPart);
        }

        if (!string.IsNullOrEmpty(company.Zipcode) || !string.IsNullOrEmpty(company.City))
        {
            var cityPart = "";
            if (!string.IsNullOrEmpty(company.Zipcode))
                cityPart = company.Zipcode;
            if (!string.IsNullOrEmpty(company.City))
                cityPart += string.IsNullOrEmpty(cityPart) ? company.City : $" {company.City}";

            if (!string.IsNullOrEmpty(cityPart))
                parts.Add(cityPart);
        }

        return string.Join(", ", parts);
    }

    private class CompanyResult
    {
        public string EnterpriseNumber { get; set; } = "";
        public string LegalForm { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Street { get; set; } = "";
        public string HouseNumber { get; set; } = "";
        public string Zipcode { get; set; } = "";
        public string City { get; set; } = "";
    }
}
