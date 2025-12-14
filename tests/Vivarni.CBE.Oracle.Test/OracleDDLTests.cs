using Vivarni.CBE.Oracle.DDL;
using Vivarni.CBE.Oracle.Setup;
using Xunit;

namespace Vivarni.CBE.Oracle.Test;

public class OracleDDLTests
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
}
