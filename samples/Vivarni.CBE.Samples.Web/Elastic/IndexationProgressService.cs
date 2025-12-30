using System.Collections.Concurrent;

namespace Vivarni.CBE.Samples.Web.Elastic;

public class IndexationProgressService
{
    private static Lock s_lock = new Lock();
    private static ConcurrentQueue<string> s_logStream = [];
    private static double s_progress_es;
    private static double s_progress_db;

    private static bool s_isReady;

    public bool IsReady()
    {
        return s_isReady;
    }

    public double Progress()
    {
        return (s_progress_db / 2) + (s_progress_es / 2);
    }

    public IEnumerable<string> LogStream()
    {
        return [];
    }

    internal static void UpdateElasticProgress(double pct)
    {
        lock (s_lock)
        {
            s_progress_es = pct;
        }
    }

    internal static void UpdateDatabaseProgress(double pct)
    {
        lock (s_lock)
        {
            s_progress_db = pct;
        }
    }
}
