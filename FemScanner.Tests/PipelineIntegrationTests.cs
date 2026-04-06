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

        var model = new BdfParser().Parse(lines);
        var results = new BdfValidator().Validate(model);
        new JsonExporter().Export(model, results, _tempDir, "sample");

        Assert.True(File.Exists(Path.Combine(_tempDir, "sample.json")));
        Assert.True(File.Exists(Path.Combine(_tempDir, "sample_validation.json")));
    }

    [Fact]
    public void JsonOutput_ContainsAllRequiredKeys()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample.bdf"));
        var model = new BdfParser().Parse(lines);
        new JsonExporter().Export(model, [], _tempDir, "sample");

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(_tempDir, "sample.json")));
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("grids", out _), "grids 키 없음");
        Assert.True(root.TryGetProperty("elements", out _), "elements 키 없음");
        Assert.True(root.TryGetProperty("properties", out _), "properties 키 없음");
        Assert.True(root.TryGetProperty("materials", out _), "materials 키 없음");
        Assert.True(root.TryGetProperty("loads", out _), "loads 키 없음");
        Assert.True(root.TryGetProperty("boundaryConditions", out _), "boundaryConditions 키 없음");
        Assert.True(root.TryGetProperty("caseControl", out _), "caseControl 키 없음");
    }

    [Fact]
    public void JsonOutput_ValidBdf_CorrectCardCounts()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample.bdf"));
        var model = new BdfParser().Parse(lines);
        new JsonExporter().Export(model, [], _tempDir, "sample");

        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(_tempDir, "sample.json")));
        var root = doc.RootElement;

        Assert.Equal(4, root.GetProperty("grids").GetArrayLength());
        Assert.Equal(2, root.GetProperty("elements").GetArrayLength());
        Assert.Equal(1, root.GetProperty("properties").GetArrayLength());
        Assert.Equal(1, root.GetProperty("materials").GetArrayLength());
    }

    [Fact]
    public void ValidationJson_ContainsSeverityField()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample_errors.bdf"));
        var model = new BdfParser().Parse(lines);
        var results = new BdfValidator().Validate(model);
        new JsonExporter().Export(model, results, _tempDir, "errors");

        string json = File.ReadAllText(Path.Combine(_tempDir, "errors_validation.json"));
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.GetArrayLength() > 0, "검증 결과가 비어 있음");
        var first = doc.RootElement[0];
        Assert.True(first.TryGetProperty("severity", out _), "severity 필드 없음");
        Assert.True(first.TryGetProperty("cardType", out _), "cardType 필드 없음");
        Assert.True(first.TryGetProperty("message", out _), "message 필드 없음");
    }

    // ── 오류 BDF 파이프라인 ───────────────────────────────────────────────────

    [Fact]
    public void ParseValidateExport_ErrorBdf_ProducesValidationErrors()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample_errors.bdf"));
        var model = new BdfParser().Parse(lines);
        var results = new BdfValidator().Validate(model);

        Assert.Contains(results, r => r.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void ParseValidateExport_ErrorBdf_MissingGridReferenceDetected()
    {
        string[] lines = File.ReadAllLines(Path.Combine(FixturesDir, "sample_errors.bdf"));
        var model = new BdfParser().Parse(lines);
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
        var model = new BdfParser().Parse(lines);
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

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }
}
