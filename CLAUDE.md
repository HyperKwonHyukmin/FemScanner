# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

**FemScanner** — MSC Nastran BDF(Bulk Data File) 분석·검증·추출 도구 (C# Console, .NET 8)

핵심 기능:
1. BDF 파일 파싱 (fixed-field 8/16자 및 free-field 콤마 방식 모두 지원)
2. Nastran 규칙 기반 문법 검증 및 오류 리포트
3. 카드별 분리 추출 → JSON 출력 (GRID, Element, Property, Material, Load, BC, Case Control)
4. 옵션: Nastran 실행 후 F06 결과 파싱 → 해석 유효성 검토

## 빌드 및 실행

```bash
# 빌드
dotnet build FemScanner/FemScanner.csproj

# 실행
dotnet run --project FemScanner -- <path/to/model.bdf>

# 테스트
dotnet test FemScanner.Tests/FemScanner.Tests.csproj

# 단일 테스트
dotnet test --filter "FullyQualifiedName~BdfParserTests.ParseGrid"
```

## 아키텍처

```
FemScanner/
├── Program.cs                  # 진입점, CLI 인자 처리
├── Parsers/
│   ├── BdfParser.cs            # BDF 전체 파싱 오케스트레이터
│   ├── CardReader.cs           # fixed/free field 분리 처리
│   └── F06Parser.cs            # F06 결과 파일 파싱
├── Models/
│   ├── BdfModel.cs             # 전체 모델 컨테이너
│   ├── CaseControl.cs          # Case Control Section
│   ├── Grids/Grid.cs
│   ├── Elements/               # CQUAD4, CTRIA3, CTETRA, CHEXA, CBAR, CBEAM, CROD 등
│   ├── Properties/             # PSHELL, PSOLID, PBAR, PBEAM, PROD 등
│   ├── Materials/              # MAT1, MAT2, MAT8 등
│   ├── Loads/                  # FORCE, MOMENT, PLOAD, PLOAD4 등
│   └── BoundaryConditions/     # SPC, SPC1, MPC 등
├── Validators/
│   ├── BdfValidator.cs         # 검증 오케스트레이터
│   └── Rules/                  # 카드별 검증 규칙
├── Exporters/
│   └── JsonExporter.cs         # BdfModel → JSON 직렬화
└── NastranRunner.cs            # Nastran 프로세스 실행 및 F06 연동
```

## BDF 파싱 핵심 규칙

- **Fixed-field**: 각 필드 8자 (첫 필드=카드명, 이후 최대 8개 필드×8자)
- **Free-field**: 콤마(`,`) 구분, 첫 토큰에 `*` 없으면 free-field
- **Large-field**: 카드명에 `*` 붙으면 16자 필드
- **Continuation**: 두 번째 필드가 `+` 또는 공백이면 연속 카드
- `$`로 시작하는 줄은 주석
- `BEGIN BULK` / `ENDDATA` 구분자로 Case Control과 Bulk Data 분리

## 출력 규칙

- JSON 출력 시 카드 타입별로 배열로 구성 (`grids[]`, `elements[]`, `properties[]` 등)
- 오류/경고는 `ValidationResult` 객체로 수집 후 콘솔 및 별도 JSON 파일로 출력
- F06 파싱 시 `FATAL`, `WARNING`, `USER WARNING` 키워드 기준으로 추출
