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
            "CREATE INDEX IX_CBE_CONTACT_ENTITY_NUMBER ON CBE_CONTACT (ENTITY_NUMBER)",
            "CREATE INDEX IX_CBE_ACTIVITY_ENTITY_NUMBER ON CBE_ACTIVITY (ENTITY_NUMBER)",
        };

        // Act
        var ddl = generator.GenerateDDL();

        // Verify the DDL contains CREATE INDEX statements
        Assert.All(sampleIndices, s => Assert.Contains(s, ddl));
    }
}
