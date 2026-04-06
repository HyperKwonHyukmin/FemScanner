using FemScanner.Parsers;

namespace FemScanner.Tests;

public class CardReaderBasicTests
{
    [Fact]
    public void ReadTokens_FixedField_Grid_ReturnsCorrectTokens()
    {
        var tokens = CardReader.ReadTokens("GRID    1       0       1.0     2.0     3.0");
        Assert.Equal(["GRID", "1", "0", "1.0", "2.0", "3.0"], tokens);
    }

    [Fact]
    public void ReadTokens_Comment_ReturnsEmpty()
    {
        var tokens = CardReader.ReadTokens("$ This is a comment");
        Assert.Empty(tokens);
    }

    [Fact]
    public void ReadTokens_BlankLine_ReturnsEmpty()
    {
        Assert.Empty(CardReader.ReadTokens(""));
        Assert.Empty(CardReader.ReadTokens("   "));
    }

    [Fact]
    public void ReadTokens_FixedField_TrimsWhitespace()
    {
        var tokens = CardReader.ReadTokens("CQUAD4  1       1       1       2       3       4");
        Assert.Equal("CQUAD4", tokens[0]);
        Assert.Equal("1", tokens[1]);
        Assert.Equal("4", tokens[6]);
    }
}
