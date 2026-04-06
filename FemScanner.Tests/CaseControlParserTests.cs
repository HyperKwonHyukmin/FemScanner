using FemScanner.Parsers;

namespace FemScanner.Tests;

public class CaseControlParserTests
{
    [Fact]
    public void Parse_Subcase_ExtractsId()
    {
        var lines = new[] { "SUBCASE 1", "  LOAD = 10", "  SPC = 20" };
        var cc = CaseControlParser.Parse(lines);

        Assert.Single(cc.Subcases);
        Assert.Equal(1, cc.Subcases[0].Id);
    }

    [Fact]
    public void Parse_Subcase_ExtractsLoadAndSpcIds()
    {
        var lines = new[] { "SUBCASE 1", "  LOAD = 10", "  SPC = 20" };
        var cc = CaseControlParser.Parse(lines);

        var sub = cc.Subcases[0];
        Assert.Equal("10", sub.Directives["LOAD"]);
        Assert.Equal("20", sub.Directives["SPC"]);
    }

    [Fact]
    public void Parse_MultipleSubcases_ExtractsBoth()
    {
        var lines = new[]
        {
            "SUBCASE 1", "  LOAD = 10",
            "SUBCASE 2", "  LOAD = 20"
        };
        var cc = CaseControlParser.Parse(lines);

        Assert.Equal(2, cc.Subcases.Count);
        Assert.Equal(1, cc.Subcases[0].Id);
        Assert.Equal(2, cc.Subcases[1].Id);
    }

    [Fact]
    public void Parse_GlobalDirectives_ExtractedBeforeSubcase()
    {
        var lines = new[] { "TITLE = My Model", "SUBCASE 1", "  LOAD = 10" };
        var cc = CaseControlParser.Parse(lines);

        Assert.True(cc.GlobalDirectives.ContainsKey("TITLE"));
        Assert.Equal("My Model", cc.GlobalDirectives["TITLE"]);
    }

    [Fact]
    public void Parse_Comments_Ignored()
    {
        var lines = new[] { "$ comment", "SUBCASE 1", "$ another", "  LOAD = 5" };
        var cc = CaseControlParser.Parse(lines);

        Assert.Single(cc.Subcases);
        Assert.Equal("5", cc.Subcases[0].Directives["LOAD"]);
    }

    [Fact]
    public void Parse_ViaBdfParser_CaseControlPopulated()
    {
        string[] bdf =
        [
            "SUBCASE 1",
            "  LOAD = 100",
            "  SPC = 200",
            "BEGIN BULK",
            "GRID    1       0       0.0     0.0     0.0",
            "ENDDATA"
        ];
        var model = new BdfParser().Parse(bdf);

        Assert.Single(model.CaseControl.Subcases);
        Assert.Equal(1, model.CaseControl.Subcases[0].Id);
        Assert.Equal("100", model.CaseControl.Subcases[0].Directives["LOAD"]);
        Assert.Equal("200", model.CaseControl.Subcases[0].Directives["SPC"]);
    }
}
