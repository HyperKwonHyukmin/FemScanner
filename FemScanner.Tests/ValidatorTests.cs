using FemScanner.Models;
using FemScanner.Models.BoundaryConditions;
using FemScanner.Models.Elements;
using FemScanner.Models.Grids;
using FemScanner.Models.Loads;
using FemScanner.Models.Materials;
using FemScanner.Models.Properties;
using FemScanner.Validators;

namespace FemScanner.Tests;

public class ValidatorTests
{
    private static BdfModel ModelWithGrids(params Grid[] grids)
    {
        var model = new BdfModel();
        foreach (var g in grids) model.Grids.Add(g);
        return model;
    }

    // ── GridRule ─────────────────────────────────────────────────────────────

    [Fact]
    public void Validate_DuplicateGridId_ReturnsError()
    {
        var model = ModelWithGrids(
            new Grid { Id = 1 },
            new Grid { Id = 1 }
        );
        var results = new BdfValidator().Validate(model);

        Assert.Contains(results, r =>
            r.Severity == ValidationSeverity.Error &&
            r.CardType == "GRID" &&
            r.CardId == 1);
    }

    [Fact]
    public void Validate_UniqueGridIds_NoError()
    {
        var model = ModelWithGrids(
            new Grid { Id = 1 },
            new Grid { Id = 2 }
        );
        var results = new BdfValidator().Validate(model);
        Assert.DoesNotContain(results, r => r.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void Validate_ZeroGridId_ReturnsError()
    {
        var model = ModelWithGrids(new Grid { Id = 0 });
        var results = new BdfValidator().Validate(model);

        Assert.Contains(results, r =>
            r.Severity == ValidationSeverity.Error &&
            r.CardType == "GRID" &&
            r.FieldName == "ID");
    }

    [Fact]
    public void Validate_EmptyModel_NoResults()
    {
        var model = new BdfModel();
        var results = new BdfValidator().Validate(model);
        Assert.Empty(results);
    }

    // ── ElementRule ───────────────────────────────────────────────────────────

    [Fact]
    public void ElementRule_MissingGrid_ReturnsError()
    {
        var model = new BdfModel();
        model.Grids.Add(new Grid { Id = 1 });
        model.Properties.Add(new PShell { Id = 1, MaterialId = 0 });
        model.Elements.Add(new CQuad4 { Id = 1, PropertyId = 1, NodeIds = [1, 2, 3, 4] }); // 2,3,4 없음

        var results = new BdfValidator().Validate(model);
        Assert.Contains(results, r => r.Severity == ValidationSeverity.Error && r.CardType == "CQUAD4");
    }

    [Fact]
    public void ElementRule_ValidGrid_ReturnsNoError()
    {
        var model = new BdfModel();
        for (int i = 1; i <= 4; i++) model.Grids.Add(new Grid { Id = i });
        model.Properties.Add(new PShell { Id = 1, MaterialId = 0 });
        model.Elements.Add(new CQuad4 { Id = 1, PropertyId = 1, NodeIds = [1, 2, 3, 4] });

        var results = new BdfValidator().Validate(model);
        Assert.DoesNotContain(results, r => r.CardType == "CQUAD4");
    }

    [Fact]
    public void ElementRule_MissingProperty_ReturnsError()
    {
        var model = new BdfModel();
        for (int i = 1; i <= 4; i++) model.Grids.Add(new Grid { Id = i });
        model.Elements.Add(new CQuad4 { Id = 1, PropertyId = 99, NodeIds = [1, 2, 3, 4] }); // Property 99 없음

        var results = new BdfValidator().Validate(model);
        Assert.Contains(results, r =>
            r.Severity == ValidationSeverity.Error &&
            r.FieldName == "PID");
    }

    // ── PropertyRule ──────────────────────────────────────────────────────────

    [Fact]
    public void PropertyRule_MissingMaterial_ReturnsError()
    {
        var model = new BdfModel();
        model.Properties.Add(new PShell { Id = 1, MaterialId = 99 }); // Material 99 없음

        var results = new BdfValidator().Validate(model);
        Assert.Contains(results, r =>
            r.Severity == ValidationSeverity.Error &&
            r.CardType == "PSHELL" &&
            r.FieldName == "MID");
    }

    [Fact]
    public void PropertyRule_ValidMaterial_NoError()
    {
        var model = new BdfModel();
        model.Materials.Add(new Mat1 { Id = 1, E = 2.1e11 });
        model.Properties.Add(new PShell { Id = 1, MaterialId = 1 });

        var results = new BdfValidator().Validate(model);
        Assert.DoesNotContain(results, r => r.CardType == "PSHELL" && r.Severity == ValidationSeverity.Error);
    }

    // ── BcRule ────────────────────────────────────────────────────────────────

    [Fact]
    public void BcRule_MissingGrid_ReturnsError()
    {
        var model = new BdfModel();
        model.BoundaryConditions.Add(new Spc { Id = 1, NodeId = 99 }); // Grid 99 없음

        var results = new BdfValidator().Validate(model);
        Assert.Contains(results, r =>
            r.Severity == ValidationSeverity.Error &&
            r.CardType == "SPC");
    }

    // ── LoadRule ──────────────────────────────────────────────────────────────

    [Fact]
    public void LoadRule_MissingGrid_ReturnsError()
    {
        var model = new BdfModel();
        model.Loads.Add(new Force { Id = 1, NodeId = 99 }); // Grid 99 없음

        var results = new BdfValidator().Validate(model);
        Assert.Contains(results, r =>
            r.Severity == ValidationSeverity.Error &&
            r.CardType == "FORCE");
    }
}
