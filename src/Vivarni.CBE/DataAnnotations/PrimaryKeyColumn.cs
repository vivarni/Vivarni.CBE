namespace Vivarni.CBE.DataAnnotations;

/// <summary>
/// Indicates that this column should be used as primary key.
/// The <see cref="ICbeDataStorage"/> is free to ignore this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class PrimaryKeyColumn : Attribute { }
