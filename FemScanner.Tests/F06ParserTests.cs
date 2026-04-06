using FemScanner.Parsers;

namespace FemScanner.Tests;

public class F06ParserTests
{
    private static F06Result Parse(params string[] lines) =>
        new F06Parser().ParseLines(lines);

    [Fact]
    public void Parse_FatalLine_ReturnsFatalMessage()
    {
        var result = Parse("FATAL ERROR: Singular matrix detected");
        Assert.Single(result.Messages);
        Assert.Equal(F06Level.Fatal, result.Messages[0].Level);
    }

    [Fact]
    public void Parse_WarningLine_ReturnsWarningMessage()
    {
        var result = Parse("WARNING: Large deformation detected");
        Assert.Single(result.Messages);
        Assert.Equal(F06Level.Warning, result.Messages[0].Level);
    }

    [Fact]
    public void Parse_UserWarningLine_ReturnsWarning()
    {
        var result = Parse("USER WARNING: Check your constraints");
        Assert.Single(result.Messages);
        Assert.Equal(F06Level.Warning, result.Messages[0].Level);
    }

    [Fact]
    public void Parse_NormalLine_ReturnsNoMessages()
    {
        var result = Parse("This is a normal output line");
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void Parse_LineNumber_IsCorrect()
    {
        var result = Parse("Normal", "Normal", "FATAL ERROR: test");
        Assert.Equal(3, result.Messages[0].LineNumber);
    }

    [Fact]
    public void Parse_Context_IncludesSurroundingLines()
    {
        var result = Parse("line1", "line2", "FATAL ERROR: test", "line4", "line5");
        Assert.Contains("line1", result.Messages[0].Context);
        Assert.Contains("FATAL ERROR", result.Messages[0].Context);
        Assert.Contains("line4", result.Messages[0].Context);
    }

    [Fact]
    public void Parse_MultipleFatals_ReturnsAll()
    {
        var result = Parse(
            "FATAL ERROR: First",
            "Normal line",
            "FATAL ERROR: Second"
        );
        Assert.Equal(2, result.FatalCount);
    }

    [Fact]
    public void Parse_MixedMessages_CountsCorrect()
    {
        var result = Parse(
            "FATAL ERROR: Fatal one",
            "WARNING: Warning one",
            "Normal",
            "USER WARNING: User warning one"
        );
        Assert.Equal(1, result.FatalCount);
        Assert.Equal(2, result.WarningCount);
    }

    [Fact]
    public void Parse_MissingFile_ThrowsFileNotFoundException()
    {
        var parser = new F06Parser();
        Assert.Throws<FileNotFoundException>(() => parser.Parse("nonexistent.f06"));
    }

    [Fact]
    public void Parse_MessageText_TrimmedCorrectly()
    {
        var result = Parse("   FATAL ERROR: Leading spaces   ");
        Assert.Equal("FATAL ERROR: Leading spaces", result.Messages[0].Message);
    }
}
