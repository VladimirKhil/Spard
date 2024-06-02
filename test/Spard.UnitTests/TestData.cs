namespace Spard.UnitTests;

public sealed class TestData : Tuple<string, string?>
{
    public TestData(string input, string? output)
        : base(input, output)
    {

    }
}
