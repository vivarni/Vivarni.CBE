# Vivarni.CBE

A simple, practical solution to import and synchronize KBO/BCE/CBE data into your own database (e.g., SQL Server, Postgres, Sqlite, Oracle, ...). It provides clear source abstractions, extensible storage backends, and synchronization state tracking so your enterprise registers stay reliably up to date. CBE is the _Crossroads Bank for Enterprises in Belgium_ and is also known as:

 * Dutch: _Kruispuntbank van Ondernemingen_ 
 * French: _Banque-Carrefour des Entreprises_
 * German: _Zentrale Datenbank der Unternehmen_

The data provided by the CBE is public available at; you simply need an account on the official website to access and download the datasets. Official federal governement website for CBE Open Data: [https://economie.fgov.be/](https://economie.fgov.be/en/themes/enterprises/crossroads-bank-enterprises/services-everyone/public-data-available-reuse/cbe-open-data)

Want to experiment without your own DB? See the sample projects in `sample/`. It demonstrates an end-to-end flow on a local SQLite and Postgres database!

## Have a local copy of CBE data; synchronized daily

1️⃣ Pull data from [the official sources](https://economie.fgov.be/en/themes/enterprises/crossroads-bank-enterprises/services-everyone/public-data-available-reuse/cbe-open-data) of the Belgian Governement.  
2️⃣ Maybe cache the files somewhere, using [some storage](https://github.com/vivarni/Vivarni.CBE/blob/master/src/Vivarni.CBE/DataStorage/ICbeDataStorage.cs) available.  
3️⃣ Store the actual data in a [database/storage](https://github.com/vivarni/Vivarni.CBE/blob/develop/src/Vivarni.CBE/DataSources/ICbeDataSourceCache.cs) of your liking  
4️⃣ Use it in your code as a repository, EF Core DbSet, or service

## Quickstart 🚀
This section shows a simplified, but concrete example of how the package can be used to create a simple that —ultimately— allows you to search CBE data locally. We have other examples in the [samples](https://github.com/vivarni/Vivarni.CBE/blob/develop/ssamples)  directory that may give more inspiration. A quick overview:
  * Search with _EF Core_ and _Sqlite_.
  * User-friendly web interface, using _Dapper_ and _Postgres_.
  * Generate SQL statements to create configurable tables (DDL)

Back to the concrete example of we can create a simple console app that allows you to search CBE data locally:

### 1. Get the data
Create an account on the [official CBE website](https://economie.fgov.be/en/themes/enterprises/crossroads-bank-enterprises/services-everyone/public-data-available-reuse/cbe-open-data). With username/password you can download the public open data. 1️⃣

### 2. Configure
After installing the packages,

```bash
dotnet add package Vivarni.CBE
dotnet add package Vivarni.CBE.SqlServer # if you're using SQL Server
dotnet add package Vivarni.CBE.Postgres  # if you're using Postgres
dotnet add package Vivarni.CBE.Sqlite    # if you're using Sqlite
dotnet add package Vivarni.CBE.Oracle    # if you're using Oracle
```
add your configuration to your `IServiceCollection`. Adjust connection string, schema, and source for your environment. You can then

```csharp
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vivarni.CBE;
using Vivarni.CBE.Sqlite;

// Configure Vivarni.CBE with the desired source or data store.
// You can easily create your own data store for more control!
var connectionString = "Data Source=c:/kbo.db";
var services = new ServiceCollection()
    .AddVivarniCBE(s => s
        .WithFtps("cbe-username", "MySecret") 1️⃣
        .WithCache("c:/temp/") 2️⃣
        .WithSqliteDatabase(connectionString) 3️⃣
    );
```
### 4. Import/sync
Then run a sync for the first time to trigger an import of your data. The method can be called anytime and will attempt to update to the latest version of CBE data. It will use partial updates when possible ~and use `ICbeSynchronisationStateRegistry` to keep track of the synchronisation state~. The CBE publishes partial update files on a daily basis.
```csharp
var provider = services.BuildServiceProvider();
var cbeService = provider.GetRequiredService<ICbeService>();
await cbeService.SyncAsync();
```

### 5. Query 4️⃣
```csharp
using Dapper;

using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

var companies = await connection.QueryAsync<(string Vat, string Name)>(
    "SELECT vat_number AS Vat, name AS Name FROM cbe_companies WHERE name LIKE @q",
    new { q = "%TECH%" }
);

foreach (var row in companies)
{
    Console.WriteLine($"{row.Vat} - {row.Name}");
}
```

## License

This project is licensed under the MIT License. Feel free to use it in your projects.
