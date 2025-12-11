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
            "IX_CbeAddress_EntityNumber",
            "IX_CbeDenomination_EntityNumber",
            "IX_CbeContact_EntityNumber"
        };

        // Act
        var ddl = generator.GenerateDDL();

        // Verify the DDL contains CREATE INDEX statements
        Assert.All(sampleIndices, s => Assert.Contains(s, ddl));
        Assert.Contains("CREATE INDEX IF NOT EXISTS", ddl);
        Assert.Contains("-- Create indexes", ddl);
    }
}
