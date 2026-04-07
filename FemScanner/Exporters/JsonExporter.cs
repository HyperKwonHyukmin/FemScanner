using System.Text.Json;
using System.Text.Json.Serialization;
using FemScanner.Models;

namespace FemScanner.Exporters;

/// <summary>
/// BdfModel과 ValidationReport를 JSON 파일로 출력합니다.
/// System.Text.Json 사용. Newtonsoft.Json 사용 금지.
/// </summary>
public class JsonExporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// BdfModel을 &lt;baseName&gt;.json으로 출력합니다.
    /// </summary>
    public void ExportModel(BdfModel model, string outputDir, string baseName)
    {
        Directory.CreateDirectory(outputDir);

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
    }

    /// <summary>
    /// 단계별 검증 보고서를 &lt;baseName&gt;_validation_step{N}.json으로 출력합니다.
    /// </summary>
    public void ExportValidation(ValidationReport report, string outputDir, string baseName)
    {
        Directory.CreateDirectory(outputDir);
        string path = Path.Combine(outputDir, $"{baseName}_validation_step{report.Step}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(report, Options));
    }
}
