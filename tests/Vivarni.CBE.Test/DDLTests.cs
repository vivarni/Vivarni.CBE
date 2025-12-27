using Vivarni.CBE.Oracle.DDL;
using Vivarni.CBE.Oracle.Setup;
using Vivarni.CBE.Postgres.DDL;
using Vivarni.CBE.Postgres.Setup;
using Vivarni.CBE.Sqlite.DDL;
using Vivarni.CBE.SqlServer.DDL;
using Xunit;

namespace Vivarni.CBE.Test;

public class DDLTests
{
    [Fact]
    public void OracleDataDefinitionLanguageGenerator_ShouldCreateIndices()
    {
        // Arrange
        var opts = new OracleCbeOptions();
        var generator = new OracleDataDefinitionLanguageGenerator(opts);
        var sampleIndices = new[]
        {
            "CREATE INDEX IX_CBE_BRANCH_ENTERPRISE_NUMBER ON CBE_BRANCH (ENTERPRISE_NUMBER)",
            "CREATE INDEX IX_CBE_ESTABLISHMENT_ENTERPRISE_NUMBER ON CBE_ESTABLISHMENT (ENTERPRISE_NUMBER)",
        };

        // Act
        var ddl = generator.GenerateDDL();

        // Verify the DDL contains CREATE INDEX statements
        Assert.All(sampleIndices, s => Assert.Contains(s, ddl));
    }

    [Fact]
    public void SqliteDataDefinitionLanguageGenerator_ShouldCreateIndices()
    {
        // Arrange
        var generator = new SqliteDataDefinitionLanguageGenerator();
        var sampleIndices = new[]
        {
            "IX_CbeBranch_EnterpriseNumber",
            "IX_CbeDenomination_EntityNumber"
        };

        // Act
        var ddl = generator.GenerateDDL();

        // Verify the DDL contains CREATE INDEX statements
        Assert.All(sampleIndices, s => Assert.Contains(s, ddl));
        Assert.Contains("CREATE INDEX IF NOT EXISTS", ddl);
    }

    [Fact]
    public void SqlServerDataDefinitionLanguageGenerator_ShouldCreateIndices()
    {
        // Arrange
        var generator = new SqlServerDataDefinitionLanguageGenerator("dbo", "");
        var sampleIndices = new[]
        {
            "IX_CbeBranch_EnterpriseNumber",
            "IX_CbeEstablishment_EnterpriseNumber"
        };

        // Act
        var ddl = generator.GenerateDDL();

        // Assert
        Assert.All(sampleIndices, s => Assert.Contains(s, ddl));
        Assert.Contains("CREATE INDEX", ddl);
        Assert.Contains("-- Create indexes", ddl);
    }

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
