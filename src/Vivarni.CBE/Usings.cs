global using Vivarni.CBE.DataAnnotations;
global using Vivarni.CBE.DataSources.Mappings;
global using CsvIndexAttribute = CsvHelper.Configuration.Attributes.IndexAttribute;
using System.Runtime.CompilerServices;

// So our unit tests can also test internal classes
[assembly: InternalsVisibleTo("Vivarni.CBE.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
