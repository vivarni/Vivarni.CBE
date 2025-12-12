using System.Text;

namespace Vivarni.CBE.Oracle.DDL;

internal class OracleDatabaseObjectNameProvider
{
    // Oracle reserved keywords - comprehensive list for Oracle Database
    private static readonly HashSet<string> s_reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "ACCESS", "ADD", "ALL", "ALTER", "AND", "ANY", "AS", "ASC", "AUDIT", "BETWEEN", "BY", "CHAR", "CHECK", 
        "CLUSTER", "COLUMN", "COLUMN_VALUE", "COMMENT", "COMPRESS", "CONNECT", "CREATE", "CURRENT", "DATE", 
        "DECIMAL", "DEFAULT", "DELETE", "DESC", "DISTINCT", "DROP", "ELSE", "EXCLUSIVE", "EXISTS", "FILE", 
        "FLOAT", "FOR", "FROM", "GRANT", "GROUP", "HAVING", "IDENTIFIED", "IMMEDIATE", "IN", "INCREMENT", 
        "INDEX", "INITIAL", "INSERT", "INTEGER", "INTERSECT", "INTO", "IS", "LEVEL", "LOCK", "LONG", "MAXEXTENTS", 
        "MINUS", "MLSLABEL", "MODE", "MODIFY", "NESTABLE", "NOAUDIT", "NOCOMPRESS", "NOT", "NOWAIT", "NULL", 
        "NUMBER", "OF", "OFFLINE", "ON", "ONLINE", "OPTION", "OR", "ORDER", "PCTFREE", "PRIOR", "PUBLIC", 
        "RAW", "RENAME", "RESOURCE", "REVOKE", "ROW", "ROWID", "ROWNUM", "ROWS", "SELECT", "SESSION", "SET", 
        "SHARE", "SIZE", "SMALLINT", "START", "SUCCESSFUL", "SYNONYM", "SYSDATE", "TABLE", "THEN", "TO", 
        "TRIGGER", "UID", "UNION", "UNIQUE", "UPDATE", "USER", "VALIDATE", "VALUES", "VARCHAR", "VARCHAR2", 
        "VIEW", "WHENEVER", "WHERE", "WITH"
    };

    public static string GetObjectName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input cannot be null or empty.", nameof(input));

        // Convert PascalCase to Oracle-style UPPER_CASE
        var upperCase = ConvertToOracleCase(input);

        return Normalize(upperCase);
    }

    private static string ConvertToOracleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder();

        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];

            // If current character is uppercase and not the first character
            if (char.IsUpper(c) && i > 0)
            {
                // Add underscore before uppercase letter, but only if the previous character
                // is not already an underscore or uppercase
                var prevChar = input[i - 1];
                if (prevChar != '_' && !char.IsUpper(prevChar))
                {
                    sb.Append('_');
                }
                // Handle sequences like "XMLParser" -> "XML_PARSER" not "X_M_L_PARSER"
                else if (i + 1 < input.Length && char.IsLower(input[i + 1]) && char.IsUpper(prevChar))
                {
                    sb.Append('_');
                }
            }

            sb.Append(char.ToUpperInvariant(c));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Normalizes an identifier to be safe for Oracle:
    /// - Handles reserved keywords by prefixing
    /// - Ensures valid Oracle identifier rules
    /// </summary>
    public static string Normalize(string identifier, bool allowReserved = false)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Identifier cannot be null/empty.", nameof(identifier));

        // Trim the identifier
        var s = identifier.Trim();

        // Replace invalid characters - Oracle allows letters, digits, underscore, $, #
        var chars = s.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var ch = chars[i];
            if (!(ch >= 'a' && ch <= 'z') &&
                !(ch >= 'A' && ch <= 'Z') &&
                !(ch >= '0' && ch <= '9') &&
                ch != '_' && ch != '$' && ch != '#')
            {
                chars[i] = '_';
            }
        }
        s = new string(chars);

        // Ensure first character is a letter
        if (s.Length == 0)
            throw new ArgumentException("Identifier became empty after normalization.", nameof(identifier));

        if (!((s[0] >= 'a' && s[0] <= 'z') || (s[0] >= 'A' && s[0] <= 'Z')))
        {
            s = "T_" + s; // prefix with T_ if starts with digit or other
        }

        // Remove consecutive underscores
        while (s.Contains("__"))
            s = s.Replace("__", "_");

        // Handle reserved keywords
        if (!allowReserved && s_reserved.Contains(s))
        {
            throw new ArgumentException($"Identifier '{identifier}' normalizes to reserved keyword '{s}'. Choose a different name.", nameof(identifier));
        }

        // Oracle identifiers have max length of 128 characters (Oracle 12.2+)
        // For compatibility with older versions, we could use 30, but 128 is more practical
        if (s.Length > 128)
        {
            s = s[..128];
        }

        return s;
    }
}
