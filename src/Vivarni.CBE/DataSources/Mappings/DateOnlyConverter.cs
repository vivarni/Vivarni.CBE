using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace Vivarni.CBE.DataSources.Mappings;

public class DateOnlyConverter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Try to parse using the expected CBE format (dd-MM-yyyy)
        if (DateOnly.TryParseExact(text, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return result;

        // Fallback to default parsing
        return base.ConvertFromString(text, row, memberMapData);
    }
}