namespace Vivarni.CBE.DataAnnotations;

/// <summary>
/// Indicates that this column should be used as primary key.
/// The <see cref="ICbeDataStorage"/> is free to ignore this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CbePrimaryKeyAttribute : Attribute
{
    /// <summary>
    /// The properties which constitute the primary key, in order.
    /// </summary>
    public IReadOnlyList<string> PropertyNames => _propertyNames.AsReadOnly();
    private readonly List<string> _propertyNames;

    public CbePrimaryKeyAttribute(string propertyName, params string[] additionalPropertyNames)
    {
        _propertyNames = [propertyName];
        _propertyNames.AddRange(additionalPropertyNames);
    }
}
