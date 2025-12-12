using System.Data;

namespace Vivarni.CBE.Util;

public static class IEnumerableExtensions
{
    public static DataTable ToDataTable<T>(this IEnumerable<T> @this)
    {
        var properties = typeof(T).GetProperties();
        var table = new DataTable();

        // Create columns using database column names from CbeMapping
        foreach (var prop in properties)
        {
            var columnName = prop.Name;
            var columnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            table.Columns.Add(columnName, columnType);
        }

        // Add rows
        foreach (var item in @this)
        {
            var row = table.NewRow();
            foreach (var prop in properties)
            {
                var columnName = prop.Name;
                var value = prop.GetValue(item);

                // Handle null values properly
                if (value == null)
                {
                    row[columnName] = DBNull.Value;
                }
                else if (value is string str && string.IsNullOrWhiteSpace(str))
                {
                    // Convert empty strings to null for nullable columns
                    var columnType = Nullable.GetUnderlyingType(prop.PropertyType);
                    row[columnName] = columnType != null ? DBNull.Value : value;
                }
                else
                {
                    row[columnName] = value;
                }
            }
            table.Rows.Add(row);
        }
        return table;
    }

    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        using (var enumerator = source.GetEnumerator())
        {
            while (enumerator.MoveNext())
            {
                yield return YieldBatchElements(enumerator, batchSize - 1);
            }
        }
    }

    private static IEnumerable<T> YieldBatchElements<T>(IEnumerator<T> source, int batchSize)
    {
        yield return source.Current;
        for (var i = 0; i < batchSize && source.MoveNext(); i++)
        {
            yield return source.Current;
        }
    }
}
