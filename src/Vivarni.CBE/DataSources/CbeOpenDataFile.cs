namespace Vivarni.CBE.DataSources;

public class CbeOpenDataFile : IEquatable<CbeOpenDataFile>
{
    public string Filename { get; }

    public DateOnly SnapshotDate { get; private set; }
    public int ExtractNumber { get; private set; }
    public CbeExtractType ExtractType { get; private set; }

    /// <summary>
    /// Creates a new instance of this class.
    /// </summary>
    public CbeOpenDataFile(string source)
    {
        Filename = Path.GetFileName(source);

        // Expected pattern: KboOpenData_[number]_[year]_[month]_[day]_[type].zip
        // Example: KboOpenData_0172_2025_11_05_Full.zip
        if (string.IsNullOrEmpty(Filename))
            throw new ArgumentException("Unable to parse OpenData filename", nameof(source));

        // Remove .zip extension if present
        var nameWithoutExtension = Filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            ? Filename[..^4]
            : Filename;

        var parts = nameWithoutExtension.Split('_');
        if (parts.Length != 6 || parts[0] != "KboOpenData")
            throw new ArgumentException($"Invalid filename format. Expected 'KboOpenData_[number]_[year]_[month]_[day]_[type].zip', got '{Filename}'");

        if (!int.TryParse(parts[1], out var number))
            throw new ArgumentException($"Invalid number format in filename: {parts[1]}");

        if (!Enum.TryParse<CbeExtractType>(parts[5], ignoreCase: true, out var type))
            throw new ArgumentException($"Invalid file type in filename: {parts[5]}. Expected 'Full' or 'Update'");

        // Parse date (year_month_day)
        if (!int.TryParse(parts[2], out var year) ||
            !int.TryParse(parts[3], out var month) ||
            !int.TryParse(parts[4], out var day))
        {
            throw new ArgumentException($"Invalid date format in filename: {parts[2]}_{parts[3]}_{parts[4]}");
        }

        try
        {
            SnapshotDate = new DateOnly(year, month, day);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ArgumentException($"Invalid date in filename: {year}-{month}-{day}", ex);
        }

        ExtractNumber = number;
        ExtractType = type;
    }

    /// <inheritdoc/>
    public override string ToString()
        => Filename;

    /// <inheritdoc/>
    public bool Equals(CbeOpenDataFile? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        return string.Equals(Filename, other.Filename, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return Equals(obj as CbeOpenDataFile);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Filename);
    }
}
