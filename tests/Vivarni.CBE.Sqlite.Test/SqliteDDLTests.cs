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
            "CREATE INDEX IF NOT EXISTS IX_CbeBranch_EnterpriseNumber ON CbeBranch (EnterpriseNumber);",
            "CREATE INDEX IF NOT EXISTS IX_CbeDenomination_EntityNumber ON CbeDenomination (EntityNumber);"
        };

        // Act
        var ddl = generator.GenerateDDL();

        // Verify the DDL contains CREATE INDEX statements
        Assert.All(sampleIndices, s => Assert.Contains(s, ddl));
        Assert.Contains("CREATE INDEX IF NOT EXISTS", ddl);
    }
}
