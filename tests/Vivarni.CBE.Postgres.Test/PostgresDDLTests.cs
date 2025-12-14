using Vivarni.CBE.Postgres.DDL;
using Vivarni.CBE.Postgres.Setup;
using Xunit;

namespace Vivarni.CBE.Postgres.Test;

public class PostgresDDLTests
{
    [Fact]
    public void PostgresDataDefinitionLanguageGenerator_ShouldCreateIndices()
    {
        // Arrange
        var opts = new PostgresCbeOptions { Schema = "public", TablePrefix = "" };
        var generator = new PostgresDataDefinitionLanguageGenerator(opts);
        var sampleIndices = new[]
        {
            "CREATE INDEX IF NOT EXISTS IX_CbeBranch_EnterpriseNumber ON public.cbe_branch (enterprise_number);",
            "CREATE INDEX IF NOT EXISTS IX_CbeEstablishment_EnterpriseNumber ON public.cbe_establishment (enterprise_number);"
        };

        // Act
        var ddl = generator.GenerateDDL();

        // Verify the DDL contains CREATE INDEX statements
        Assert.All(sampleIndices, s => Assert.Contains(s, ddl));
        Assert.Contains("CREATE INDEX IF NOT EXISTS", ddl);
        Assert.Contains("-- Create indexes", ddl);
    }
}
