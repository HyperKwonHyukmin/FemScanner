using FemScanner.Exporters;
using FemScanner.Models;
using FemScanner.Parsers;
using FemScanner.Validators;

// ── 인자 파싱 ─────────────────────────────────────────────────────────────────

if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
{
    PrintUsage();
    Environment.Exit(args.Length == 0 ? 1 : 0);
}

string bdfPath = args[0];
bool runNastran = false;

for (int i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--nastran":
            runNastran = true;
            break;
        default:
            Console.Error.WriteLine($"알 수 없는 옵션: {args[i]}");
            PrintUsage();
            Environment.Exit(1);
            break;
    }
}

// ── 입력 파일 검증 ────────────────────────────────────────────────────────────

if (!File.Exists(bdfPath))
{
    Console.Error.WriteLine($"오류: 파일을 찾을 수 없습니다: {bdfPath}");
    Environment.Exit(1);
}

string ext = Path.GetExtension(bdfPath).ToLowerInvariant();
if (ext != ".bdf" && ext != ".dat")
{
    Console.Error.WriteLine($"경고: 지원되지 않는 확장자 '{ext}'. .bdf 또는 .dat 파일을 사용하세요.");
}

string baseName = Path.GetFileNameWithoutExtension(bdfPath);
string outputDir = Path.GetDirectoryName(Path.GetFullPath(bdfPath)) ?? ".";

// ── 파이프라인 실행 ───────────────────────────────────────────────────────────

try
{
    FemScanner.Parsers.F06Result? f06Result = null;

    Console.WriteLine($"파싱 중: {bdfPath}");
    string[] lines = File.ReadAllLines(bdfPath);
    BdfModel model = new BdfParser().Parse(lines);
    Console.WriteLine($"  GRID: {model.Grids.Count}개, Elements: {model.Elements.Count}개, " +
                      $"Properties: {model.Properties.Count}개, Materials: {model.Materials.Count}개");
    if (model.Warnings.Count > 0)
        Console.WriteLine($"  파서 경고: {model.Warnings.Count}건");

    Console.WriteLine("검증 중...");
    IReadOnlyList<ValidationResult> results = new BdfValidator().Validate(model);
    int errorCount = results.Count(r => r.Severity == ValidationSeverity.Error);
    int warningCount = results.Count(r => r.Severity == ValidationSeverity.Warning);
    Console.WriteLine($"  Errors: {errorCount}, Warnings: {warningCount}");

    Console.WriteLine($"JSON 출력 중: {outputDir}");
    var exporter = new JsonExporter();
    exporter.Export(model, results, outputDir, baseName);
    Console.WriteLine($"  {baseName}.json 생성 완료");
    Console.WriteLine($"  {baseName}_validation.json 생성 완료");

    // ── Nastran 연동 ──────────────────────────────────────────────────────────
    if (runNastran)
    {
        Console.WriteLine("Nastran 실행 중...");
        var runner = new FemScanner.NastranRunner.NastranRunner();
        string f06Path = runner.Run(bdfPath);

        Console.WriteLine($"F06 파싱 중: {f06Path}");
        f06Result = new F06Parser().Parse(f06Path);
        exporter.ExportF06(f06Result, outputDir, baseName);
        Console.WriteLine($"  {baseName}_f06_summary.json 생성 완료");
        Console.WriteLine($"  F06 Fatal: {f06Result.FatalCount}건, Warning: {f06Result.WarningCount}건");
    }

    // ── 오류 통합 요약 ────────────────────────────────────────────────────────
    int f06FatalCount = f06Result?.FatalCount ?? 0;
    if (errorCount > 0 || warningCount > 0 || f06FatalCount > 0)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("=== 오류 요약 ===================================================");

        // BDF 검증 결과
        Console.Error.WriteLine($"[BDF 검증] Errors: {errorCount}, Warnings: {warningCount}");
        foreach (var r in results)
        {
            if (r.Severity == ValidationSeverity.Error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                string field = string.IsNullOrEmpty(r.FieldName) ? "" : $" ({r.FieldName})";
                Console.Error.WriteLine($"  [Error] {r.CardType} #{r.CardId}{field} - {r.Message}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                string field = string.IsNullOrEmpty(r.FieldName) ? "" : $" ({r.FieldName})";
                Console.Error.WriteLine($"  [Warning] {r.CardType} #{r.CardId}{field} - {r.Message}");
                Console.ResetColor();
            }
        }

        // F06 Fatal 메시지
        if (f06Result is not null && f06FatalCount > 0)
        {
            Console.Error.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"[F06 FATAL] {f06FatalCount}건");
            Console.ResetColor();
            foreach (var msg in f06Result.Messages.Where(m => m.Level == F06Level.Fatal))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"  [{msg.LineNumber}행] {msg.Message}");
                Console.ResetColor();
                Console.Error.WriteLine("    컨텍스트:");
                foreach (string ctxLine in msg.Context.Split(Environment.NewLine))
                    Console.Error.WriteLine($"      {ctxLine}");
                Console.Error.WriteLine();
            }
        }

        // 총계
        int totalErrors = errorCount + f06FatalCount;
        if (f06FatalCount > 0)
            Console.Error.WriteLine($"총 오류: {totalErrors}건 (BDF {errorCount} + F06 Fatal {f06FatalCount})");
        else
            Console.Error.WriteLine($"총 오류: {errorCount}건 (BDF), 경고: {warningCount}건");

        Console.Error.WriteLine("=================================================================");
    }

    Console.WriteLine("완료.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"오류: {ex.Message}");
    Environment.Exit(1);
}

static void PrintUsage()
{
    Console.WriteLine("""
        사용법: dotnet run --project FemScanner -- <path/to/model.bdf> [옵션]

        옵션:
          --nastran    Nastran 실행 활성화 (F06 파싱 포함, PATH에 nastran 명령 필요)
          --help       이 도움말 출력

        출력 파일 (BDF와 동일 폴더에 생성):
          <name>.json              파싱된 BDF 모델 데이터
          <name>_validation.json   검증 결과 (오류/경고)
          <name>_f06_summary.json  F06 결과 요약 (--nastran 옵션 시)
        """);
}
