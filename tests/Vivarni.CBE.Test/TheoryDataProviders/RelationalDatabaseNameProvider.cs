using System.Collections;
using Xunit;

namespace Vivarni.CBE.Test.TheoryDataProviders;

public class RelationalDatabaseNameProvider : IEnumerable<TheoryDataRow<string>>
{
    private readonly IList<TheoryDataRow<string>> _data =
    [
        new TheoryDataRow<string>("postgres"),
        new TheoryDataRow<string>("oracle"),
        new TheoryDataRow<string>("sqlserver"),
        new TheoryDataRow<string>("sqlite"),
    ];

    public IEnumerator<TheoryDataRow<string>> GetEnumerator() => _data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

