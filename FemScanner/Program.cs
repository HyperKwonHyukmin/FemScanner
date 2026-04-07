using FemScanner.Exporters;
using FemScanner.Helpers;
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
    Console.Error.WriteLine($"경고: 지원되지 않는 확장자 '{ext}'. .bdf 또는 .dat 파일을 사용하세요.");

string baseName = Path.GetFileNameWithoutExtension(bdfPath);
string outputDir = Path.GetDirectoryName(Path.GetFullPath(bdfPath)) ?? ".";

// ── 파이프라인 실행 ───────────────────────────────────────────────────────────

try
{
    // ── 파싱 ─────────────────────────────────────────────────────────────────
    Console.WriteLine($"파싱 중: {bdfPath}");
    string[] lines = File.ReadAllLines(bdfPath);
    BdfModel model = new BdfParser().Parse(lines);
    Console.WriteLine($"  GRID: {model.Grids.Count}개, Elements: {model.Elements.Count}개, " +
                      $"Properties: {model.Properties.Count}개, Materials: {model.Materials.Count}개");
    if (model.Warnings.Count > 0)
        Console.WriteLine($"  파서 경고: {model.Warnings.Count}건");

    // ── 파싱 요약 구성 ────────────────────────────────────────────────────────

    // BoundingBox: GRID 좌표 범위
    var boundingBox = model.Grids.Count > 0
        ? new BoundingBox
          {
              XMin = model.Grids.Min(g => g.X), XMax = model.Grids.Max(g => g.X),
              YMin = model.Grids.Min(g => g.Y), YMax = model.Grids.Max(g => g.Y),
              ZMin = model.Grids.Min(g => g.Z), ZMax = model.Grids.Max(g => g.Z),
          }
        : new BoundingBox();

    // Orphan 계산
    var referencedGridIds = new HashSet<int>(
        model.Elements.SelectMany(e => e.NodeIds)
        .Concat(model.Loads.OfType<FemScanner.Models.Loads.Force>().Select(f => f.NodeId))
        .Concat(model.Loads.OfType<FemScanner.Models.Loads.Moment>().Select(m => m.NodeId))
        .Concat(model.BoundaryConditions.OfType<FemScanner.Models.BoundaryConditions.Spc>().Select(s => s.NodeId))
        .Concat(model.BoundaryConditions.OfType<FemScanner.Models.BoundaryConditions.Spc1>().SelectMany(s => s.NodeIds))
        .Concat(model.BoundaryConditions.OfType<FemScanner.Models.BoundaryConditions.Mpc>().SelectMany(s => s.Terms.Select(t => t.NodeId)))
        .Where(id => id != 0));

    var referencedPropertyIds = model.Elements.Select(e => e.PropertyId).Where(id => id != 0).ToHashSet();
    var referencedMaterialIds = model.Properties
        .Select(p => p switch
        {
            FemScanner.Models.Properties.PShell  x => x.MaterialId,
            FemScanner.Models.Properties.PSolid  x => x.MaterialId,
            FemScanner.Models.Properties.PBar    x => x.MaterialId,
            FemScanner.Models.Properties.PBarL   x => x.MaterialId,
            FemScanner.Models.Properties.PBeam   x => x.MaterialId,
            FemScanner.Models.Properties.PBeamL  x => x.MaterialId,
            FemScanner.Models.Properties.PRod    x => x.MaterialId,
            _ => 0,
        })
        .Where(id => id != 0).ToHashSet();

    int orphanNodes      = model.Grids.Count(g => !referencedGridIds.Contains(g.Id));
    int orphanProperties = model.Properties.Count(p => !referencedPropertyIds.Contains(p.Id));
    int orphanMaterials  = model.Materials.Count(m => !referencedMaterialIds.Contains(m.Id));

    var parsingSummary = new ParsingSummary
    {
        CardCounts = new Dictionary<string, int>
        {
            ["grid"]              = model.Grids.Count,
            ["element"]           = model.Elements.Count,
            ["property"]          = model.Properties.Count,
            ["material"]          = model.Materials.Count,
            ["load"]              = model.Loads.Count,
            ["boundaryCondition"] = model.BoundaryConditions.Count,
            ["subcase"]           = model.CaseControl.Subcases.Count,
            ["param"]             = model.Params.Count,
        },
        ElementBreakdown  = model.Elements.GroupBy(e => e.CardType).ToDictionary(g => g.Key, g => g.Count()),
        PropertyBreakdown = model.Properties.GroupBy(p => p.CardType).ToDictionary(g => g.Key, g => g.Count()),
        MaterialBreakdown = model.Materials.GroupBy(m => m.CardType).ToDictionary(g => g.Key, g => g.Count()),
        LoadBreakdown     = model.Loads.GroupBy(l => l.CardType).ToDictionary(g => g.Key, g => g.Count()),
        BcBreakdown       = model.BoundaryConditions.GroupBy(b => b.CardType).ToDictionary(g => g.Key, g => g.Count()),
        ParserWarnings    = model.Warnings.ToList(),
        BoundingBox       = boundingBox,
        OrphanNodes       = orphanNodes,
        OrphanProperties  = orphanProperties,
        OrphanMaterials   = orphanMaterials,
    };

    if (orphanNodes > 0 || orphanProperties > 0 || orphanMaterials > 0)
        Console.WriteLine($"  Orphan — 노드: {orphanNodes}개, 물성: {orphanProperties}개, 재질: {orphanMaterials}개");

    // ── [단계 1] BDF 기본 검토 ────────────────────────────────────────────────
    Console.WriteLine("[단계 1] BDF 기본 검토 중...");
    var validator = new BdfValidator();
    var (results, ruleChecks) = validator.ValidateDetailed(model);
    int errorCount   = results.Count(r => r.Severity == ValidationSeverity.Error);
    int warningCount = results.Count(r => r.Severity == ValidationSeverity.Warning);
    Console.WriteLine($"  Errors: {errorCount}, Warnings: {warningCount}");

    string step1Status = errorCount > 0 ? "error" : warningCount > 0 ? "warning" : "pass";
    var step1Report = new ValidationReport
    {
        Step            = 1,
        StepName        = "BDF 기본 검토",
        GeneratedAt     = DateTimeOffset.Now,
        SourceFile      = Path.GetFileName(bdfPath),
        Status          = step1Status,
        Summary = new ValidationSummary
        {
            TotalErrors    = errorCount,
            TotalWarnings  = warningCount,
            ParserWarnings = model.Warnings.Count,
        },
        ParsingSummary    = parsingSummary,
        RulesChecked      = ruleChecks.ToList(),
        ValidationResults = results.ToList(),
    };

    Console.WriteLine($"JSON 출력 중: {outputDir}");
    var exporter = new JsonExporter();
    exporter.ExportModel(model, outputDir, baseName);
    Console.WriteLine($"  {baseName}.json 생성 완료");

    exporter.ExportValidation(step1Report, outputDir, baseName);
    Console.WriteLine($"  {baseName}_validation_step1.json 생성 완료 (status: {step1Status})");

    // ── [단계 2] Nastran 해석 검토 ────────────────────────────────────────────
    if (runNastran)
    {
        string tempDir      = Path.GetTempPath();
        string checkBdfPath = Path.Combine(tempDir, $"{baseName}_check.bdf");
        string? checkF06Path = null;

        try
        {
            Console.WriteLine("[단계 2] 검증용 임시 BDF 생성 중 (하중 제거 + GRAV 적용)...");
            string[] checkLines = BdfCheckFileBuilder.BuildCheckLines(lines, model);
            File.WriteAllLines(checkBdfPath, checkLines);

            Console.WriteLine("[단계 2] Nastran 실행 중...");
            var runner = new FemScanner.NastranRunner.NastranRunner();
            checkF06Path = runner.Run(checkBdfPath);

            Console.WriteLine($"[단계 2] F06 파싱 중: {checkF06Path}");
            F06Result f06Result = new F06Parser().Parse(checkF06Path);
            Console.WriteLine($"  F06 Fatal: {f06Result.FatalCount}건, Warning: {f06Result.WarningCount}건");

            var f06Section = new F06Section
            {
                FatalCount   = f06Result.FatalCount,
                WarningCount = f06Result.WarningCount,
                Messages     = f06Result.Messages.Select(m => new F06MessageDto
                {
                    Level      = m.Level.ToString().ToLowerInvariant(),
                    LineNumber = m.LineNumber,
                    Message    = m.Message,
                    Context    = m.Context,
                }).ToList(),
            };

            string step2Status = f06Result.FatalCount > 0 ? "error"
                               : f06Result.WarningCount > 0 ? "warning"
                               : "pass";

            var step2Report = new ValidationReport
            {
                Step        = 2,
                StepName    = "Nastran 해석 검토",
                GeneratedAt = DateTimeOffset.Now,
                SourceFile  = Path.GetFileName(bdfPath),
                Status      = step2Status,
                Summary = new ValidationSummary
                {
                    F06Fatals   = f06Result.FatalCount,
                    F06Warnings = f06Result.WarningCount,
                },
                F06Summary = f06Section,
            };

            exporter.ExportValidation(step2Report, outputDir, baseName);
            Console.WriteLine($"  {baseName}_validation_step2.json 생성 완료 (status: {step2Status})");

            // F06 Fatal 콘솔 요약
            if (f06Result.FatalCount > 0)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("=== [단계 2] F06 오류 요약 =====================================");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"[F06 FATAL] {f06Result.FatalCount}건");
                Console.ResetColor();
                foreach (var msg in f06Section.Messages.Where(m => m.Level == "fatal"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"  [{msg.LineNumber}행] {msg.Message}");
                    Console.ResetColor();
                    Console.Error.WriteLine("    컨텍스트:");
                    foreach (string ctxLine in msg.Context.Split(Environment.NewLine))
                        Console.Error.WriteLine($"      {ctxLine}");
                    Console.Error.WriteLine();
                }
                Console.Error.WriteLine("=================================================================");
            }
        }
        finally
        {
            TryDelete(checkBdfPath);
            if (checkF06Path is not null) TryDelete(checkF06Path);
        }
    }

    // ── [단계 1] BDF 오류 콘솔 요약 ──────────────────────────────────────────
    if (errorCount > 0 || warningCount > 0)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("=== [단계 1] BDF 검증 오류 요약 ================================");
        Console.Error.WriteLine($"Errors: {errorCount}, Warnings: {warningCount}");
        foreach (var r in results)
        {
            Console.ForegroundColor = r.Severity == ValidationSeverity.Error
                ? ConsoleColor.Red : ConsoleColor.Yellow;
            string field = string.IsNullOrEmpty(r.FieldName) ? "" : $" ({r.FieldName})";
            string level = r.Severity == ValidationSeverity.Error ? "Error" : "Warning";
            Console.Error.WriteLine($"  [{level}] {r.CardType} #{r.CardId}{field} - {r.Message}");
            Console.ResetColor();
        }
        Console.Error.WriteLine("=================================================================");
    }

    Console.WriteLine("완료.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"오류: {ex.Message}");
    Environment.Exit(1);
}

static void TryDelete(string path)
{
    try { if (File.Exists(path)) File.Delete(path); }
    catch { /* 정리 실패는 무시 */ }
}

static void PrintUsage()
{
    Console.WriteLine("""
        사용법: dotnet run --project FemScanner -- <path/to/model.bdf> [옵션]

        옵션:
          --nastran    Nastran 실행 활성화 (F06 파싱 포함, PATH에 nastran 명령 필요)
                       * 하중 카드(FORCE, MOMENT, PLOAD, PLOAD4)를 제거하고 GRAV만
                         적용한 임시 BDF로 해석 → 모델 자체 구조 유효성만 검증
          --help       이 도움말 출력

        출력 파일 (BDF와 동일 폴더에 생성):
          <name>.json                    파싱된 BDF 모델 데이터
          <name>_validation_step1.json   [단계 1] BDF 기본 검토 결과
          <name>_validation_step2.json   [단계 2] Nastran 해석 검토 결과 (--nastran 시)
        """);
}
