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

        // Act
        var ddl = generator.GenerateDDL();

        // Assert - Check that some sample indices are included in the DDL
        var sampleIndices = new[]
        {
            "IX_CbeAddress_EntityNumber",
            "IX_CbeDenomination_EntityNumber",
            "IX_CbeContact_EntityNumber"
        };

        foreach (var indexName in sampleIndices)
        {
            Assert.Contains(indexName, ddl);
        }

        // Verify the DDL contains CREATE INDEX statements
        Assert.Contains("CREATE INDEX IF NOT EXISTS", ddl);
        Assert.Contains("-- Create indexes", ddl);
    }
}
