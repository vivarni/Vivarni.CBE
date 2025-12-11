using System.Text;

namespace Vivarni.CBE.Postgres;

internal class DatabaseObjectNameProvider
{
    // Minimal reserved keywords set—expand for your domain, or leave empty and enforce naming rules upstream.
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "user", "order", "select", "group", "table", "column", "constraint",
        "primary", "foreign", "references", "where", "join", "index"
    };

    public static string GetObjectName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or empty.", nameof(input));

        // Convert PascalCase to snake_case
        var snakeCase = ConvertToSnakeCase(input);

        return Normalize(snakeCase);
    }

    private static string ConvertToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            // If current character is uppercase and not the first character
            if (char.IsUpper(c) && i > 0)
            {
                // Add underscore before uppercase letter, but only if the previous character
                // is not already an underscore or uppercase
                char prevChar = input[i - 1];
                if (prevChar != '_' && !char.IsUpper(prevChar))
                {
                    sb.Append('_');
                }
                // Handle sequences like "XMLParser" -> "xml_parser" not "x_m_l_parser"
                else if (i + 1 < input.Length && char.IsLower(input[i + 1]) && char.IsUpper(prevChar))
                {
                    sb.Append('_');
                }
            }

            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Normalizes an identifier to be safe, unquoted, and case-insensitive in PostgreSQL:
    /// - Lowercase
    /// - Only letters, digits, underscore
    /// - Starts with letter or underscore
    /// Throws if it cannot produce a safe identifier or if it's a reserved keyword (unless allowReserved).
    /// </summary>
    public static string Normalize(string identifier, bool allowReserved = false)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null/empty.", nameof(identifier));

        // Trim and lowercase
        var s = identifier.Trim().ToLowerInvariant();

        // Replace invalid characters with underscore
        var chars = s.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char ch = chars[i];
            if (!(ch >= 'a' && ch <= 'z') &&
                !(ch >= '0' && ch <= '9') &&
                ch != '_')
            {
                chars[i] = '_';
            }
        }
        s = new string(chars);

        // Ensure first character is letter or underscore
        if (s.Length == 0)
            throw new ArgumentException("Identifier became empty after normalization.", nameof(identifier));

        if (!((s[0] >= 'a' && s[0] <= 'z') || s[0] == '_'))
        {
            s = "_" + s; // prefix underscore if starts with digit or other
        }

        // Avoid consecutive underscores normalization artifacts (optional)
        while (s.Contains("__"))
            s = s.Replace("__", "_");

        // Optional: enforce not reserved
        if (!allowReserved && Reserved.Contains(s))
            throw new ArgumentException($"Identifier '{identifier}' normalizes to reserved keyword '{s}'. Choose a different name.", nameof(identifier));

        return s; // Unquoted, case-insensitive friendly
    }
}
