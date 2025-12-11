using Vivarni.CBE.Sqlite.DDL;
using Xunit;

namespace Vivarni.CBE.Sqlite.Test;

public class SqliteDDLTests
{
    [Fact]
    public void SqliteDataDefinitionLanguageGenerator_ShouldCreateIndices()
    {
        // Arrange
        var generator = new SqliteDataDefinitionLanguageGenerator();
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
