using FemScanner.Parsers;

namespace FemScanner.Tests;

public class CardReaderTests
{
    // ── fixed-field ──────────────────────────────────────────────────────────

    [Fact]
    public void ReadTokens_FixedField_ReturnsCorrectTokens()
    {
        var tokens = CardReader.ReadTokens("GRID    1       0       1.0     2.0     3.0");
        Assert.Equal(["GRID", "1", "0", "1.0", "2.0", "3.0"], tokens);
    }

    [Fact]
    public void ReadTokens_FixedField_TrimsWhitespace()
    {
        var tokens = CardReader.ReadTokens("CQUAD4  1       1       1       2       3       4");
        Assert.Equal("CQUAD4", tokens[0]);
        Assert.Equal("1", tokens[1]);
        Assert.Equal("4", tokens[6]);
    }

    [Fact]
    public void ReadTokens_FixedField_NoTrailingEmptyTokens()
    {
        var tokens = CardReader.ReadTokens("GRID    1");
        Assert.Equal(["GRID", "1"], tokens);
    }

    // ── free-field ───────────────────────────────────────────────────────────

    [Fact]
    public void ReadTokens_FreeField_ReturnsCorrectTokens()
    {
        var tokens = CardReader.ReadTokens("GRID,1,0,1.0,2.0,3.0");
        Assert.Equal(["GRID", "1", "0", "1.0", "2.0", "3.0"], tokens);
    }

    [Fact]
    public void ReadTokens_FreeField_TrimsSpaces()
    {
        var tokens = CardReader.ReadTokens("GRID, 1 , 0 , 1.0 , 2.0 , 3.0");
        Assert.Equal(["GRID", "1", "0", "1.0", "2.0", "3.0"], tokens);
    }

    // ── large-field ──────────────────────────────────────────────────────────

    [Fact]
    public void ReadTokens_LargeField_Returns16CharTokens()
    {
        // large-field: * 접두어, 16자 필드
        string line = "*GRID           1               0               1.0             ";
        var tokens = CardReader.ReadTokens(line);
        Assert.True(tokens.Length >= 2, "large-field는 최소 카드명+데이터 반환");
        Assert.Equal("*GRID", tokens[0]);
        Assert.Equal("1", tokens[1]);
    }

    [Fact]
    public void ReadTokens_LargeField_StarPrefix_Detected()
    {
        var tokens = CardReader.ReadTokens("*GRID           100");
        Assert.Equal("*GRID", tokens[0]);
        Assert.Equal("100", tokens[1]);
    }

    // ── comment / blank ──────────────────────────────────────────────────────

    [Fact]
    public void ReadTokens_Comment_ReturnsEmpty()
    {
        Assert.Empty(CardReader.ReadTokens("$ This is a comment"));
        Assert.Empty(CardReader.ReadTokens("$"));
    }

    [Fact]
    public void ReadTokens_BlankLine_ReturnsEmpty()
    {
        Assert.Empty(CardReader.ReadTokens(""));
        Assert.Empty(CardReader.ReadTokens("   "));
    }

    // ── continuation (ReadCards) ─────────────────────────────────────────────

    [Fact]
    public void ReadCards_Continuation_MergesTokens()
    {
        var lines = new[]
        {
            "GRID    1       0       1.0     2.0     3.0     +GR1",
            "+GR1    0       0"
        };
        var cards = CardReader.ReadCards(lines);
        Assert.Single(cards);
        // 병합된 카드: GRID 1 0 1.0 2.0 3.0 +GR1 0 0 (마커 제외된 데이터)
        Assert.Contains("GRID", cards[0]);
        Assert.Contains("0", cards[0]);
    }

    [Fact]
    public void ReadCards_NoContinuation_ReturnsTwoCards()
    {
        var lines = new[]
        {
            "GRID    1       0       1.0     2.0     3.0",
            "GRID    2       0       4.0     5.0     6.0"
        };
        var cards = CardReader.ReadCards(lines);
        Assert.Equal(2, cards.Count);
        Assert.Equal("GRID", cards[0][0]);
        Assert.Equal("GRID", cards[1][0]);
    }

    [Fact]
    public void ReadCards_CommentLines_Skipped()
    {
        var lines = new[]
        {
            "$ comment",
            "GRID    1       0       1.0     2.0     3.0",
            "$ another comment"
        };
        var cards = CardReader.ReadCards(lines);
        Assert.Single(cards);
        Assert.Equal("GRID", cards[0][0]);
    }

    [Fact]
    public void ReadCards_EmptyInput_ReturnsEmpty()
    {
        var cards = CardReader.ReadCards([]);
        Assert.Empty(cards);
    }
}
