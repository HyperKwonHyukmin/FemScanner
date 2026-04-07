using System.Text.Json;
using FemScanner.Exporters;
using FemScanner.Models;
using FemScanner.Parsers;
using FemScanner.Validators;

namespace FemScanner.Tests;

public class PipelineIntegrationTests : IDisposable
{
    private static readonly string FixturesDir =
        Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    // ── 정상 BDF 파이프라인 ───────────────────────────────────────────────────

    [Fact]
    public void ParseValidateExport_ValidBdf_ProducesJsonFiles()
    {
        string bdfPath = Path.Combine(FixturesDir, "sample.bdf");
        string[] lines = File.ReadAllLines(bdfPath);

        var model     = new BdfParser().Parse(lines);
        var validator = new BdfValidator();
        var results   = validator.Validate(model);

        new JsonExporter().ExportModel(model, _tempDir, "sample");
        new JsonExporter().ExportValidation(BuildReport(model, results, validator), _tempDir, "sample");

        Assert.True(File.Exists(Path.Combine(_tempDir, "sample.json")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "sample_validation_step1.json")));
    }

    [Fact]
    public void JsonOutput_ContainsAllRequiredKeys()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample.bdf"));
        var model = new BdfParser().Parse(lines);
        new JsonExporter().ExportModel(model, _tempDir, "sample");

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(_tempDir, "sample.json")));
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("grids", out _),              "grids 키 없음");
        Assert.True(root.TryGetProperty("elements", out _),           "elements 키 없음");
        Assert.True(root.TryGetProperty("properties", out _),         "properties 키 없음");
        Assert.True(root.TryGetProperty("materials", out _),          "materials 키 없음");
        Assert.True(root.TryGetProperty("loads", out _),              "loads 키 없음");
        Assert.True(root.TryGetProperty("boundaryConditions", out _), "boundaryConditions 키 없음");
        Assert.True(root.TryGetProperty("caseControl", out _),        "caseControl 키 없음");
    }

    [Fact]
    public void JsonOutput_ValidBdf_CorrectCardCounts()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample.bdf"));
        var model = new BdfParser().Parse(lines);
        new JsonExporter().ExportModel(model, _tempDir, "sample");

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(_tempDir, "sample.json")));
        var root = doc.RootElement;

        Assert.Equal(4, root.GetProperty("grids").GetArrayLength());
        Assert.Equal(2, root.GetProperty("elements").GetArrayLength());
        Assert.Equal(1, root.GetProperty("properties").GetArrayLength());
        Assert.Equal(1, root.GetProperty("materials").GetArrayLength());
    }

    [Fact]
    public void ValidationJson_ContainsReportStructure()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample_errors.bdf"));
        var model     = new BdfParser().Parse(lines);
        var validator = new BdfValidator();
        var results   = validator.Validate(model);
        new JsonExporter().ExportValidation(BuildReport(model, results, validator), _tempDir, "errors");

        string json = File.ReadAllText(Path.Combine(_tempDir, "errors_validation_step1.json"));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("status", out _),            "status 필드 없음");
        Assert.True(root.TryGetProperty("summary", out _),           "summary 필드 없음");
        Assert.True(root.TryGetProperty("parsingSummary", out _),    "parsingSummary 필드 없음");
        Assert.True(root.TryGetProperty("rulesChecked", out _),      "rulesChecked 필드 없음");
        Assert.True(root.TryGetProperty("validationResults", out _), "validationResults 필드 없음");

        var validationResults = root.GetProperty("validationResults");
        Assert.True(validationResults.GetArrayLength() > 0, "검증 결과가 비어 있음");

        var first = validationResults[0];
        Assert.True(first.TryGetProperty("severity", out _), "severity 필드 없음");
        Assert.True(first.TryGetProperty("cardType", out _), "cardType 필드 없음");
        Assert.True(first.TryGetProperty("message", out _),  "message 필드 없음");
    }

    [Fact]
    public void ValidationJson_ValidBdf_StatusIsPass()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample.bdf"));
        var model     = new BdfParser().Parse(lines);
        var validator = new BdfValidator();
        var results   = validator.Validate(model);
        new JsonExporter().ExportValidation(BuildReport(model, results, validator), _tempDir, "sample");

        string json = File.ReadAllText(Path.Combine(_tempDir, "sample_validation_step1.json"));
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("pass", doc.RootElement.GetProperty("status").GetString());
    }

    // ── 오류 BDF 파이프라인 ───────────────────────────────────────────────────

    [Fact]
    public void ParseValidateExport_ErrorBdf_ProducesValidationErrors()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample_errors.bdf"));
        var model   = new BdfParser().Parse(lines);
        var results = new BdfValidator().Validate(model);

        Assert.Contains(results, r => r.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void ParseValidateExport_ErrorBdf_MissingGridReferenceDetected()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample_errors.bdf"));
        var model   = new BdfParser().Parse(lines);
        var results = new BdfValidator().Validate(model);

        // CQUAD4가 Grid 2,3,4를 참조하지만 Grid 1만 존재 → Error
        Assert.Contains(results, r =>
            r.Severity == ValidationSeverity.Error &&
            r.CardType == "CQUAD4");
    }

    [Fact]
    public void ParseValidateExport_ErrorBdf_MissingMaterialReferenceDetected()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample_errors.bdf"));
        var model   = new BdfParser().Parse(lines);
        var results = new BdfValidator().Validate(model);

        // PSHELL이 Material 99 참조하지만 존재하지 않음 → Error
        Assert.Contains(results, r =>
            r.Severity == ValidationSeverity.Error &&
            r.CardType == "PSHELL" &&
            r.FieldName == "MID");
    }

    [Fact]
    public void CaseControl_ValidBdf_SubcaseParsed()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample.bdf"));
        var model = new BdfParser().Parse(lines);

        Assert.Single(model.CaseControl.Subcases);
        Assert.Equal(1, model.CaseControl.Subcases[0].Id);
        Assert.Equal("10", model.CaseControl.Subcases[0].Directives["LOAD"]);
        Assert.Equal("20", model.CaseControl.Subcases[0].Directives["SPC"]);
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
            ParsingSummary  = new ParsingSummary
            {
                CardCounts        = new Dictionary<string, int> { ["grid"] = model.Grids.Count },
                ElementBreakdown  = model.Elements.GroupBy(e => e.CardType).ToDictionary(g => g.Key, g => g.Count()),
                ParserWarnings    = model.Warnings.ToList(),
            },
            RulesChecked    = validator.RuleNames.Select(n => new RuleCheckResult { Rule = n, Status = "pass" }).ToList(),
            ValidationResults = results.ToList(),
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
