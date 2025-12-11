namespace Vivarni.CBE.DataSources;

public class CbeOpenDataFile : IEquatable<CbeOpenDataFile>
{
    public string Filename { get; }
    public string Source { get; private set; }

    public DateOnly Date { get; private set; }
    public int Number { get; private set; }
    public CbeOpenDataFileType Type { get; private set; }

    public CbeOpenDataFile(string source)
    {
        Source = source;
        Filename = Path.GetFileName(Source);

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

        if (!Enum.TryParse<CbeOpenDataFileType>(parts[5], ignoreCase: true, out var type))
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
            Date = new DateOnly(year, month, day);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ArgumentException($"Invalid date in filename: {year}-{month}-{day}", ex);
        }

        Number = number;
        Type = type;
    }

    public override string ToString()
        => Filename;

    public bool Equals(CbeOpenDataFile? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null)
            return false;

        return string.Equals(Filename, other.Filename, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CbeOpenDataFile);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Filename);
    }
}
