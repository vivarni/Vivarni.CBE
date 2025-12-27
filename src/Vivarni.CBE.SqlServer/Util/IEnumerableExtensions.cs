using System.Data;

namespace Vivarni.CBE.SqlServer.Util;

internal static class IEnumerableExtensions
{
    /// <summary>
    /// Converts a sequence of objects to a <see cref="DataTable"/> where each
    /// public property of <typeparamref name="T"/> becomes a column.
    /// </summary>
    /// <typeparam name="T">Type of objects contained in the sequence.</typeparam>
    /// <param name="this">The source sequence to convert.</param>
    /// <returns>A <see cref="DataTable"/> representing the sequence. Returns an empty
    /// table when the sequence contains no elements (columns are created from the type).</returns>
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
}
