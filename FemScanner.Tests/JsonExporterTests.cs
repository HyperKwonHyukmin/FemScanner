using System.Text.Json;
using FemScanner.Exporters;
using FemScanner.Models;
using FemScanner.Models.Elements;
using FemScanner.Models.Grids;
using FemScanner.Models.Materials;
using FemScanner.Models.Properties;
using FemScanner.Parsers;
using FemScanner.Validators;

namespace FemScanner.Tests;

public class JsonExporterTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    [Fact]
    public void ExportModel_Json_ContainsRequiredKeys()
    {
        var model = BuildSampleModel();
        new JsonExporter().ExportModel(model, _tempDir, "test");

        string json = File.ReadAllText(Path.Combine(_tempDir, "test.json"));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("grids", out _));
        Assert.True(root.TryGetProperty("elements", out _));
        Assert.True(root.TryGetProperty("properties", out _));
        Assert.True(root.TryGetProperty("materials", out _));
        Assert.True(root.TryGetProperty("loads", out _));
        Assert.True(root.TryGetProperty("boundaryConditions", out _));
        Assert.True(root.TryGetProperty("caseControl", out _));
    }

    [Fact]
    public void ExportValidation_Json_ContainsRequiredFields()
    {
        var results = new List<ValidationResult>
        {
            new() { Severity = ValidationSeverity.Error, CardType = "GRID", CardId = 1, FieldName = "ID", Message = "중복" }
        };
        var report = BuildReport(new BdfModel(), results, new BdfValidator());
        new JsonExporter().ExportValidation(report, _tempDir, "test");

        string json = File.ReadAllText(Path.Combine(_tempDir, "test_validation_step1.json"));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("status", out _));
        Assert.True(root.TryGetProperty("summary", out _));
        Assert.True(root.TryGetProperty("parsingSummary", out _));
        Assert.True(root.TryGetProperty("rulesChecked", out _));
        Assert.True(root.TryGetProperty("validationResults", out _));

        var first = root.GetProperty("validationResults")[0];
        Assert.True(first.TryGetProperty("severity", out _));
        Assert.True(first.TryGetProperty("cardType", out _));
        Assert.True(first.TryGetProperty("message", out _));
    }

    [Fact]
    public void ExportModel_Json_IsValidJson()
    {
        var model = BuildSampleModel();
        new JsonExporter().ExportModel(model, _tempDir, "test");

        string json = File.ReadAllText(Path.Combine(_tempDir, "test.json"));
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
    }

    [Fact]
    public void ExportModel_PolymorphicElement_ContainsCardType()
    {
        var model = new BdfModel();
        model.Grids.Add(new Grid { Id = 1 });
        model.Elements.Add(new CQuad4 { Id = 1, PropertyId = 1, NodeIds = [1, 1, 1, 1] });
        new JsonExporter().ExportModel(model, _tempDir, "test");

        string json = File.ReadAllText(Path.Combine(_tempDir, "test.json"));
        Assert.Contains("CQUAD4", json);
    }

    [Fact]
    public void ExportValidation_NoErrors_StatusIsPass()
    {
        var model = new BdfModel();
        var report = BuildReport(model, [], new BdfValidator());
        new JsonExporter().ExportValidation(report, _tempDir, "model");

        string json = File.ReadAllText(Path.Combine(_tempDir, "model_validation_step1.json"));
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("pass", doc.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public void ExportValidation_IntegrationWithParser_ProducesFiles()
    {
        string[] bdf =
        [
            "SUBCASE 1", "  LOAD = 10",
            "BEGIN BULK",
            "GRID    1       0       0.0     0.0     0.0",
            "PSHELL  1       1       0.01",
            "MAT1    1       2.1E11  0.0     0.3",
            "ENDDATA"
        ];
        var model = new BdfParser().Parse(bdf);
        var validator = new BdfValidator();
        var results = validator.Validate(model);

        new JsonExporter().ExportModel(model, _tempDir, "model");
        new JsonExporter().ExportValidation(BuildReport(model, results, validator), _tempDir, "model");

        Assert.True(File.Exists(Path.Combine(_tempDir, "model.json")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "model_validation_step1.json")));
    }

    private static ValidationReport BuildReport(
        BdfModel model,
        IReadOnlyList<ValidationResult> results,
        BdfValidator validator)
    {
        int errors   = results.Count(r => r.Severity == ValidationSeverity.Error);
        int warnings = results.Count(r => r.Severity == ValidationSeverity.Warning);
        string status = errors > 0 ? "error" : warnings > 0 ? "warning" : "pass";
        return new ValidationReport
        {
            Step            = 1,
            StepName        = "BDF 기본 검토",
            GeneratedAt     = DateTimeOffset.Now,
            SourceFile      = "test.bdf",
            Status          = status,
            Summary         = new ValidationSummary { TotalErrors = errors, TotalWarnings = warnings },
            ParsingSummary  = new ParsingSummary { ParserWarnings = model.Warnings.ToList() },
            RulesChecked    = validator.RuleNames.Select(n => new RuleCheckResult { Rule = n, Status = "pass" }).ToList(),
            ValidationResults = results.ToList(),
        };
    }

    private static BdfModel BuildSampleModel()
    {
        var model = new BdfModel();
        model.Grids.Add(new Grid { Id = 1, X = 1.0, Y = 2.0, Z = 3.0 });
        model.Materials.Add(new Mat1 { Id = 1, E = 2.1e11, Nu = 0.3 });
        model.Properties.Add(new PShell { Id = 1, MaterialId = 1, Thickness = 0.01 });
        model.Elements.Add(new CQuad4 { Id = 1, PropertyId = 1, NodeIds = [1, 1, 1, 1] });
        return model;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
