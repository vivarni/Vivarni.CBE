using Vivarni.CBE.SqlServer.DDL;
using Xunit;

namespace Vivarni.CBE.SqlServer.Test;

public class DDLTests
{
    [Fact]
    public void SqlServerDataDefinitionLanguageGenerator_ShouldCreateIndices()
    {
        // Arrange
        var generator = new SqlServerDataDefinitionLanguageGenerator("dbo", "");
        
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
        Assert.Contains("CREATE INDEX", ddl);
        Assert.Contains("-- Create indexes", ddl);
    }
}
