using System.Text.Json;
using FemScanner.Models;
using FemScanner.Parsers;

namespace FemScanner.Exporters;

/// <summary>
/// BdfModel과 ValidationResult를 JSON 파일로 출력합니다.
/// System.Text.Json 사용. Newtonsoft.Json 사용 금지.
/// </summary>
public class JsonExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// BdfModel을 &lt;baseName&gt;.json으로, ValidationResult를 &lt;baseName&gt;_validation.json으로 출력합니다.
    /// </summary>
    /// <param name="model">파싱된 BDF 모델</param>
    /// <param name="results">검증 결과 목록</param>
    /// <param name="outputDir">출력 디렉토리</param>
    /// <param name="baseName">출력 파일 기본명 (확장자 제외)</param>
    public void Export(BdfModel model, IReadOnlyList<ValidationResult> results,
                       string outputDir, string baseName)
    {
        Directory.CreateDirectory(outputDir);

        // 모델 JSON 출력
        string modelPath = Path.Combine(outputDir, $"{baseName}.json");
        var modelOutput = new
        {
            grids = model.Grids,
            elements = model.Elements,
            properties = model.Properties,
            materials = model.Materials,
            loads = model.Loads,
            boundaryConditions = model.BoundaryConditions,
            caseControl = model.CaseControl,
            parameters = model.Params,
        };
        File.WriteAllText(modelPath, JsonSerializer.Serialize(modelOutput, Options));

        // 검증 결과 JSON 출력
        string validationPath = Path.Combine(outputDir, $"{baseName}_validation.json");
        File.WriteAllText(validationPath, JsonSerializer.Serialize(results, Options));
    }

    /// <summary>F06 파싱 결과를 &lt;baseName&gt;_f06_summary.json으로 출력합니다.</summary>
    public void ExportF06(F06Result f06Result, string outputDir, string baseName)
    {
        Directory.CreateDirectory(outputDir);
        string f06Path = Path.Combine(outputDir, $"{baseName}_f06_summary.json");
        var output = new
        {
            fatalCount = f06Result.FatalCount,
            warningCount = f06Result.WarningCount,
            messages = f06Result.Messages,
        };
        File.WriteAllText(f06Path, JsonSerializer.Serialize(output, Options));
    }
}
