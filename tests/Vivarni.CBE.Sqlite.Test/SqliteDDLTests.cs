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
