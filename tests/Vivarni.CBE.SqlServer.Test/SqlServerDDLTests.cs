using Vivarni.CBE.SqlServer.DDL;
using Xunit;

namespace Vivarni.CBE.SqlServer.Test;

public class SqlServerDDLTests
{
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
}
