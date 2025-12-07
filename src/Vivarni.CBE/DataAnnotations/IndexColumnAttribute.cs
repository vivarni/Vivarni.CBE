namespace Vivarni.CBE.DataAnnotations;

/// <summary>
/// Indicates that this column should be indexed in a relational database (frequent lookups).
/// The <see cref="ICbeDataStorage"/> is free to ignore this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class IndexColumnAttribute : Attribute { }
