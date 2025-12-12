using Xunit;

namespace Vivarni.CBE.Postgres.Test;

public class PostgresIntegrationTest
{
    [Fact(Skip = "Not Implemented")]
    public async Task Postgres_FullCircle()
    {
        // 1. Create ZIP-files with fake data and store them in the git repo
        // 2. Configure Vivarni.CBE with postgres using only cache (no real source)
        // 3. The cache should contain the pre-made ZIP-files
        // 4. Execute Sync
        // 5. Query the database and assert
    }
}
