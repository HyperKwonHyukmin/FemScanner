using FemScanner.Models.Elements;
using FemScanner.Models.Materials;
using FemScanner.Models.Properties;
using FemScanner.Parsers;

namespace FemScanner.Tests;

public class BdfParserTests
{
    private static readonly string[] SampleBdf =
    [
        "$ FemScanner sample BDF",
        "SUBCASE 1",
        "  LOAD = 1",
        "BEGIN BULK",
        "$ Bulk data section",
        "GRID    1       0       1.0     2.0     3.0",
        "GRID    2       0       4.0     5.0     6.0",
        "UNKNOWN 99",
        "ENDDATA"
    ];

    [Fact]
    public void Parse_SectionSplit_OnlyBulkDataParsed()
    {
        var parser = new BdfParser();
        var model = parser.Parse(SampleBdf);
        // GRID 2개 파싱 성공
        Assert.Equal(2, model.Grids.Count);
    }

    [Fact]
    public void Parse_Grid_CorrectValues()
    {
        var parser = new BdfParser();
        var model = parser.Parse(SampleBdf);

        var g1 = model.Grids.First(g => g.Id == 1);
        Assert.Equal(1, g1.Id);
        Assert.Equal(0, g1.CoordId);
        Assert.Equal(1.0, g1.X);
        Assert.Equal(2.0, g1.Y);
        Assert.Equal(3.0, g1.Z);
    }

    [Fact]
    public void Parse_UnsupportedCard_AddsWarning()
    {
        var parser = new BdfParser();
        var model = parser.Parse(SampleBdf);

        Assert.Contains(model.Warnings, w => w.Contains("UNKNOWN"));
    }

    [Fact]
    public void Parse_EmptyLines_Ignored()
    {
        string[] lines =
        [
            "BEGIN BULK",
            "",
            "   ",
            "$ comment",
            "GRID    1       0       0.0     0.0     0.0",
            "ENDDATA"
        ];
        var model = new BdfParser().Parse(lines);
        Assert.Single(model.Grids);
    }

    [Fact]
    public void Parse_NoBeginBulk_StartsBulkAtFirstSupportedBulkCard()
    {
        string[] lines =
        [
            "SOL 101",
            "CEND",
            "  LOAD = 1",
            "PARAM,GRDPNT,0",
            "GRID           1         77590.0 11020.0 27646.0"
        ];
        var model = new BdfParser().Parse(lines);
        Assert.Single(model.Params);
        var grid = Assert.Single(model.Grids);
        Assert.Equal(1, grid.Id);
        Assert.Equal(77590.0, grid.X);
        Assert.Equal(11020.0, grid.Y);
        Assert.Equal(27646.0, grid.Z);
    }

    [Fact]
    public void Parse_AfterEnddata_Ignored()
    {
        string[] lines =
        [
            "BEGIN BULK",
            "GRID    1       0       0.0     0.0     0.0",
            "ENDDATA",
            "GRID    2       0       1.0     1.0     1.0"
        ];
        var model = new BdfParser().Parse(lines);
        Assert.Single(model.Grids);
    }

    // ── 전체 지원 카드 통합 검증 ────────────────────────────────────────────

    [Fact]
    public void Parse_AllSupportedElements_Parsed()
    {
        string[] lines =
        [
            "BEGIN BULK",
            "GRID    1       0       0.0     0.0     0.0",
            "GRID    2       0       1.0     0.0     0.0",
            "GRID    3       0       1.0     1.0     0.0",
            "GRID    4       0       0.0     1.0     0.0",
            "CQUAD4  1       1       1       2       3       4",
            "CTRIA3  2       1       1       2       3",
            "CROD    3       2       1       2",
            "CBAR    4       3       1       2       0",
            "CBEAM   5       3       1       2       0",
            "ENDDATA"
        ];
        var model = new BdfParser().Parse(lines);

        Assert.Equal(5, model.Elements.Count);
        Assert.IsType<CQuad4>(model.Elements[0]);
        Assert.IsType<CTria3>(model.Elements[1]);
        Assert.IsType<CRod>(model.Elements[2]);
        Assert.IsType<CBar>(model.Elements[3]);
        Assert.IsType<CBeam>(model.Elements[4]);
    }

    [Fact]
    public void Parse_AllSupportedProperties_Parsed()
    {
        string[] lines =
        [
            "BEGIN BULK",
            "PSHELL  1       1       0.01",
            "PSOLID  2       1",
            "PBAR    3       1       1.0     2.0     3.0     4.0",
            "PROD    4       1       1.0     2.0",
            "ENDDATA"
        ];
        var model = new BdfParser().Parse(lines);

        Assert.Equal(4, model.Properties.Count);
        Assert.IsType<PShell>(model.Properties[0]);
        Assert.IsType<PSolid>(model.Properties[1]);
        Assert.IsType<PBar>(model.Properties[2]);
        Assert.IsType<PRod>(model.Properties[3]);
    }

    [Fact]
    public void Parse_PBeamL_WithContinuation_ParsesDimensions()
    {
        string[] lines =
        [
            "BEGIN BULK",
            "PBEAML         1       1               L",
            "           130.0   130.0    12.0    12.0     0.0",
            "ENDDATA"
        ];

        var model = new BdfParser().Parse(lines);
        var prop = Assert.IsType<PBeamL>(Assert.Single(model.Properties));

        Assert.Equal(1, prop.Id);
        Assert.Equal("L", prop.Type);
        Assert.Equal([130.0, 130.0, 12.0, 12.0, 0.0], prop.Dimensions);
    }

    [Fact]
    public void Parse_PBarL_WithContinuation_ParsesDimensions()
    {
        string header =
            "PBARL".PadRight(8) +
            "7".PadRight(8) +
            "3".PadRight(8) +
            "".PadRight(8) +
            "BOX".PadRight(8);
        string continuation =
            "".PadRight(8) +
            "100.0".PadRight(8) +
            "50.0".PadRight(8) +
            "8.0".PadRight(8) +
            "8.0".PadRight(8) +
            "0.0".PadRight(8);

        string[] lines =
        [
            "BEGIN BULK",
            header,
            continuation,
            "ENDDATA"
        ];

        var model = new BdfParser().Parse(lines);
        var prop = Assert.IsType<PBarL>(Assert.Single(model.Properties));

        Assert.Equal(7, prop.Id);
        Assert.Equal("BOX", prop.Type);
        Assert.Equal([100.0, 50.0, 8.0, 8.0, 0.0], prop.Dimensions);
    }

    [Fact]
    public void Parse_AllSupportedMaterials_Parsed()
    {
        string[] lines =
        [
            "BEGIN BULK",
            "MAT1    1       2.1E11  0.0     0.3",
            "MAT2    2       1.0     0.0     0.0     1.0     0.0     1.0",
            "MAT8    3       1.0     1.0     0.3     0.5     0.5     0.5",
            "ENDDATA"
        ];
        var model = new BdfParser().Parse(lines);

        Assert.Equal(3, model.Materials.Count);
        Assert.IsType<Mat1>(model.Materials[0]);
        Assert.IsType<Mat2>(model.Materials[1]);
        Assert.IsType<Mat8>(model.Materials[2]);
    }

    [Fact]
    public void Parse_UnsupportedCard_WarningContainsLineNumber()
    {
        string[] lines =
        [
            "BEGIN BULK",
            "GRID    1       0       0.0     0.0     0.0",
            "UNKNOWN 99"
        ];
        var model = new BdfParser().Parse(lines);

        Assert.Single(model.Warnings);
        Assert.Contains("Line", model.Warnings[0]);
        Assert.Contains("UNKNOWN", model.Warnings[0]);
    }
}
