namespace Vivarni.CBE.DataSources;

public interface ICbeEntity
{
    /// <summary>
    /// Primary key to keep the relational database sane. Data from CBE can sometimes be a bit quirky,
    /// in order to avoid blabla...
    /// </summary>
    public long CbeId { get; set; }
}
