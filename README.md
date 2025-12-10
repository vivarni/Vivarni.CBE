# Vivarni.CBE

A simple, practical solution to import and synchronize CBE/KBO data into your own database (e.g., SQL Server, Postgres, Oracle, ...). It provides clear source abstractions, extensible storage backends, and synchronization state tracking so your enterprise registers stay reliably up to date.

The term CBE refers to Belgium’s Crossroads Bank for Enterprises (KBO/BCE). The data provided by the CBE is public available at; you simply need an account on the official website to access and download the datasets. Official website:  https://economie.fgov.be/en/themes/enterprises/crossroads-bank-enterprises/services-everyone/public-data-available-reuse/cbe-open-data

Want to experiment without your own DB? See the sample projects in `sample/`. It demonstrates an end-to-end flow on a local SQLite and Postgres database!

## Features ✨

- Data Sources: file system and HTTP providers for CBE Open Data.
- Storage Backends: interfaces and implementations for relational DBs (SQL Server, Sqlite).
- Easy extensibility to other database types via the `ICbeDataStorage` interface.
- Schema & Entities: strongly-typed `Entities` with mappings and descriptors.
- Synchronization State: import tracking via `ICbeSynchronisationStateRegistry` implementations.
- A sample application showing end-to-end flow. 🖥️

## Quickstart 🏁

### 1) Get the data 📥
Create an account on the [official CBE website](https://economie.fgov.be/en/themes/enterprises/crossroads-bank-enterprises/services-everyone/public-data-available-reuse/cbe-open-data). With username/password you can download the public open data.

### 2) Install via NuGet 📦
Consume the package from your own solution via NuGet.

```bash
dotnet add package Vivarni.CBE
dotnet add package Vivarni.CBE.SqlServer
```

### 3) Example (Sqlite) 🗄️

Configure services and run a sync. Adjust connection string, schema, and source for your environment. You can then

```csharp
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Vivarni.CBE;
using Vivarni.CBE.Sqlite;

// Configure Vivarni.CBE with the desired source or data store.
// You can easily create your own data store for more control!
var connectionString = "Data Source=c:/kbo.db";
var services = new ServiceCollection()
    .AddVivarniCBE(s => s
        .WithSqliteDatabase(connectionString)
        .WithHttpSource("cbe-username", "MySecret")
    );

var provider = services.BuildServiceProvider();
var cbeService = provider.GetRequiredService<ICbeService>();

// Trigger a manual import/sync
await cbeService.SyncAsync();

// Query your local copy of the CBE database
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

## License 📄

This project is licensed under the MIT License. Feel free to use it in your projects.