using FemScanner.Models.Elements;
using FemScanner.Models.Loads;
using FemScanner.Models.BoundaryConditions;
using FemScanner.Parsers;

namespace FemScanner.Tests;

public class BdfParserIntegrationTests
{
    private static readonly string[] MixedBdf =
    [
        "$ Mixed BDF integration test",
        "SUBCASE 1",
        "  LOAD = 10",
        "  SPC = 20",
        "BEGIN BULK",
        "$ Grids",
        "GRID    1       0       0.0     0.0     0.0",
        "GRID    2       0       1.0     0.0     0.0",
        "GRID    3       0       1.0     1.0     0.0",
        "GRID    4       0       0.0     1.0     0.0",
        "$ Element",
        "CQUAD4  1       1       1       2       3       4",
        "$ Property",
        "PSHELL  1       1       0.01",
        "$ Material",
        "MAT1    1       2.1E11  0.0     0.3     7850.0",
        "$ Load",
        "FORCE   10      1       1       0       1000.0  0.0     0.0     1.0",
        "$ BC",
        "SPC1    20      123456  1",
        "ENDDATA"
    ];

    [Fact]
    public void Parse_MixedBdf_AllCardTypesParsed()
    {
        var model = new BdfParser().Parse(MixedBdf);

        Assert.Equal(4, model.Grids.Count);
        Assert.Single(model.Elements);
        Assert.Single(model.Properties);
        Assert.Single(model.Materials);
        Assert.Single(model.Loads);
        Assert.Single(model.BoundaryConditions);
    }

    [Fact]
    public void Parse_MixedBdf_ElementIsCorrectType()
    {
        var model = new BdfParser().Parse(MixedBdf);
        Assert.IsType<CQuad4>(model.Elements[0]);
        Assert.Equal(4, model.Elements[0].NodeIds.Length);
    }

    [Fact]
    public void Parse_MixedBdf_ForceLoadParsed()
    {
        var model = new BdfParser().Parse(MixedBdf);
        var force = Assert.IsType<Force>(model.Loads[0]);
        Assert.Equal(10, force.Id);
        Assert.Equal(1000.0, force.Magnitude);
    }

    [Fact]
    public void Parse_MixedBdf_Spc1BcParsed()
    {
        var model = new BdfParser().Parse(MixedBdf);
        var spc1 = Assert.IsType<Spc1>(model.BoundaryConditions[0]);
        Assert.Equal(20, spc1.Id);
        Assert.Equal("123456", spc1.Dof);
        Assert.Contains(1, spc1.NodeIds);
    }

    [Fact]
    public void Parse_MixedBdf_NoWarningsForSupportedCards()
    {
        var model = new BdfParser().Parse(MixedBdf);
        Assert.Empty(model.Warnings);
    }
}
