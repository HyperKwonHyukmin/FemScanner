# FemScanner 개발 PRD

## 🎯 핵심 정보

**목적**: MSC Nastran BDF 파일을 파싱·검증·추출하여 구조 데이터를 JSON으로 출력하고, 선택적으로 Nastran 해석 후 F06 결과로 모델 유효성을 검토하는 CLI 도구 신규 개발
**대상**: Program, BdfParser, CardReader, BdfModel, Validators, JsonExporter, F06Parser, NastranRunner 전체 신규 구현

---

## 🔍 현재 상태 분석

### 현재 구조

신규 개발 프로젝트. 현재 C# 소스 파일 없음. `CLAUDE.md`에 아키텍처 설계 문서만 존재함.

- 언어/플랫폼: C# Console, .NET 8
- 계획된 구조: Parser → Model → Validator → Exporter → (Optional) NastranRunner 파이프라인

### 신규 개발 필요 이유

MSC Nastran BDF는 독자적 고정폭/자유형식 혼합 포맷으로, 범용 파서가 없어 FEM 엔지니어가 수동으로 카드 데이터를 확인해야 한다. 자동화 도구로 아래 문제를 해결한다:

- **검증 부재**: BDF 오류는 Nastran 실행 실패 시에야 발견 → 사전 문법 검증 필요
- **데이터 추출 어려움**: 카드별 수동 파싱 → 구조화된 JSON 자동 추출 필요
- **해석 유효성 확인 비효율**: F06 수동 확인 → 자동 파싱 및 요약 필요

### 식별된 요구사항 갭

| 갭 | 설명 |
|----|------|
| **BDF 포맷 복잡성** | fixed-field(8자), large-field(16자, `*` 접두어), free-field(콤마), continuation 카드 혼재 |
| **카드 종류 다양성** | GRID, 7종 요소, 5종 속성, 3종 재료, 4종 하중, 3종 BC, Case Control 각각 별도 파싱 필요 |
| **검증 규칙 복잡성** | 카드별 필드 수, 타입, 참조 무결성(GRID ID 존재 여부 등) 규칙 상이 |
| **F06 결과 해석** | FATAL/WARNING 키워드 패턴 기반 추출 및 구조화 필요 |

---

## ⚡ 개선 요구사항

### 1. 핵심 개발 요구사항

| ID | 요구사항 | 설명 | 필수 이유 | 대상 모듈 |
|----|----------|------|-----------|-----------|
| **R001** | BDF 포맷 파싱 | fixed(8자/16자) + free-field(콤마) + continuation 카드 완전 지원 | 입력 처리의 기반 | CardReader, BdfParser |
| **R002** | Case Control 파싱 | BEGIN BULK 이전 섹션 분리 및 SUBCASE, LOAD, SPC, METHOD 등 파싱 | 해석 설정 추출 필수 | BdfParser, CaseControl |
| **R003** | Bulk Data 카드 모델화 | GRID, Element, Property, Material, Load, BC 각각 독립 모델 클래스 | 데이터 구조화 기반 | BdfModel, 각 카드 Model 클래스 |
| **R004** | Nastran 문법 검증 | 카드별 필드 수/타입/참조 무결성 규칙 검사 및 오류 리포트 | BDF 오류 사전 감지 | BdfValidator, Rules |
| **R005** | JSON 출력 | grids, elements, properties, materials, loads, bcs, caseControl 배열로 직렬화 | 데이터 활용성 | JsonExporter |
| **R006** | ValidationResult 출력 | 오류/경고를 콘솔 및 별도 JSON 파일로 출력 | 검증 결과 가시화 | BdfValidator, JsonExporter |
| **R007** | Nastran 실행 (옵션) | 외부 Nastran 프로세스 실행 및 F06 파일 생성 대기 | 해석 자동화 | NastranRunner |
| **R008** | F06 결과 파싱 | FATAL, WARNING, USER WARNING 키워드 기반 추출 및 구조화 | 해석 유효성 검토 | F06Parser |
| **R009** | CLI 인터페이스 | `dotnet run -- <bdf파일>` 형식, `--nastran`, `--output` 옵션 지원 | 사용 편의성 | Program |

### 2. 품질 요구사항

| ID | 요구사항 | 설명 | 필수 이유 | 대상 모듈 |
|----|----------|------|-----------|-----------|
| **R010** | 단위 테스트 | 파서, 검증기, 익스포터 주요 경로 단위 테스트 작성 | 회귀 방지 | FemScanner.Tests |
| **R011** | 에러 핸들링 | 파일 없음, 빈 파일, 잘못된 포맷 등 경계 조건 처리 | 안정성 | BdfParser, Program |

### 3. 이번 범위에서 제외 (다음 단계)

- **시각화**: 3D 모델 뷰어 — 별도 GUI 프로젝트 필요
- **BDF 수정/저장**: 파싱 및 추출에 집중, 편집 기능은 다음 단계
- **Nastran 라이선스 관리**: 외부 환경 의존, 범위 초과
- **MAT8 이방성 재료 완전 지원**: 초기 버전은 MAT1 위주, 순차 확장

---

## 🗺️ 영향 범위

```
📦 직접 개발 대상
├── 🔧 Program
│   └── 요구사항: R009 (CLI 진입점, 옵션 파싱)
├── 🔧 CardReader
│   └── 요구사항: R001 (fixed/free/large-field 라인 분리)
├── 🔧 BdfParser
│   └── 요구사항: R001, R002 (전체 파싱 오케스트레이터)
├── 🔧 CaseControl
│   └── 요구사항: R002 (Case Control 섹션 모델)
├── 🔧 BdfModel + 카드 Model 클래스들
│   └── 요구사항: R003 (Grid, Element, Property, Material, Load, BC 모델)
├── 🔧 BdfValidator + Rules
│   └── 요구사항: R004, R006 (문법 검증 및 결과 수집)
├── 🔧 JsonExporter
│   └── 요구사항: R005, R006 (JSON 직렬화 및 파일 출력)
├── 🔧 NastranRunner
│   └── 요구사항: R007 (외부 프로세스 실행)
└── 🔧 F06Parser
    └── 요구사항: R008 (F06 결과 파싱)

📦 간접 영향 대상
└── ⚠️ FemScanner.Tests
    └── 영향: 모든 모듈의 공개 API 변경 시 테스트 수정 필요

🔒 변경 불가 (외부 의존)
├── 🚫 MSC Nastran 실행 파일 — 외부 라이선스 도구, 인터페이스만 정의
└── 🚫 .NET 8 BCL — 플랫폼 제약
```

---

## 📄 모듈별 상세 개발 내용

### CardReader

> **구현 요구사항:** `R001` | **변경 유형:** 신규 구현

| 항목 | 내용 |
|------|------|
| **역할** | BDF 파일의 각 라인을 읽어 fixed-field / free-field / large-field 방식으로 토큰 배열 분리 |
| **현재 문제** | 미구현 |
| **개선 내용** | • 8자 고정폭 슬라이싱으로 fixed-field 토큰 추출 (최대 10필드)<br>• `*` 접두어 감지 시 16자 large-field 모드 전환<br>• 콤마 포함 시 free-field(Split(',')) 처리<br>• `+` 또는 공백 두 번째 필드 감지로 continuation 카드 병합<br>• `$` 시작 라인 주석 처리 (스킵) |
| **완료 기준** | • fixed-field 8자 슬라이싱 정확도 100%<br>• free-field 콤마 분리 정확도 100%<br>• large-field 16자 분리 정확도 100%<br>• continuation 카드 병합 후 단일 토큰 배열 반환 확인 |
| **구현 요구사항 ID** | `R001` |

---

### BdfParser

> **구현 요구사항:** `R001`, `R002` | **변경 유형:** 신규 구현

| 항목 | 내용 |
|------|------|
| **역할** | BDF 파일 전체를 읽어 Case Control / Bulk Data 섹션 분리 후 CardReader로 카드별 토큰화, 각 모델 파서로 디스패치하는 오케스트레이터 |
| **현재 문제** | 미구현 |
| **개선 내용** | • `BEGIN BULK` / `ENDDATA` 구분자로 섹션 분리<br>• Case Control 섹션 → CaseControlParser 위임<br>• Bulk Data 카드명 기반 switch/dictionary 디스패치<br>• 미지원 카드는 경고 수집 후 스킵<br>• 파싱 결과를 `BdfModel`에 집적 |
| **완료 기준** | • BEGIN BULK 이전/이후 정확히 분리됨<br>• GRID, CQUAD4, PSHELL, MAT1, FORCE, SPC 각 1개 이상 파싱 성공<br>• 미지원 카드 경고 리스트 생성 확인 |
| **구현 요구사항 ID** | `R001`, `R002` |

---

### CaseControl

> **구현 요구사항:** `R002` | **변경 유형:** 신규 구현

| 항목 | 내용 |
|------|------|
| **역할** | Case Control 섹션의 SUBCASE, LOAD, SPC, METHOD, DISP, STRESS 등 지시문 파싱 및 모델 저장 |
| **현재 문제** | 미구현 |
| **개선 내용** | • `키워드 = 값` 또는 `키워드(옵션) = 값` 패턴 파싱<br>• SUBCASE 블록별 계층 구조 구성<br>• 파싱된 지시문을 Dictionary<string, string> 또는 구조체로 저장<br>• JSON 직렬화 가능한 모델로 설계 |
| **완료 기준** | • SUBCASE 1개 이상 포함 BDF에서 SUBCASE ID, LOAD ID, SPC ID 정확히 추출<br>• CaseControl 객체 JSON 직렬화 정상 동작 |
| **구현 요구사항 ID** | `R002` |

---

### BdfModel + 카드 Model 클래스들

> **구현 요구사항:** `R003` | **변경 유형:** 신규 구현

| 항목 | 내용 |
|------|------|
| **역할** | 파싱된 모든 BDF 카드 데이터의 구조화된 컨테이너. 카드 타입별 List<T> 보유 |
| **현재 문제** | 미구현 |
| **개선 내용** | • `BdfModel`: `List<Grid>`, `List<IElement>`, `List<IProperty>`, `List<IMaterial>`, `List<ILoad>`, `List<IBoundaryCondition>`, `CaseControl` 포함<br>• `Grid`: ID, X, Y, Z, CoordID, OutCoordID<br>• Elements: `CQUAD4`(4노드), `CTRIA3`(3노드), `CTETRA`(4노드), `CHEXA`(8노드), `CBAR`, `CBEAM`, `CROD` — 공통 인터페이스 `IElement`<br>• Properties: `PSHELL`, `PSOLID`, `PBAR`, `PBEAM`, `PROD` — `IProperty`<br>• Materials: `MAT1`, `MAT2`, `MAT8` — `IMaterial`<br>• Loads: `FORCE`, `MOMENT`, `PLOAD`, `PLOAD4` — `ILoad`<br>• BCs: `SPC`, `SPC1`, `MPC` — `IBoundaryCondition` |
| **완료 기준** | • 모든 카드 모델이 JSON 직렬화 가능<br>• `[JsonPolymorphic]` 또는 커스텀 컨버터로 다형성 직렬화 동작<br>• BdfModel에서 카드 타입별 필터링/조회 가능 |
| **구현 요구사항 ID** | `R003` |

---

### BdfValidator + Rules

> **구현 요구사항:** `R004`, `R006` | **변경 유형:** 신규 구현

| 항목 | 내용 |
|------|------|
| **역할** | 파싱된 BdfModel에 대해 Nastran 규칙 기반 검증 수행, ValidationResult 목록 반환 |
| **현재 문제** | 미구현 |
| **개선 내용** | • `ValidationResult`: Severity(Error/Warning), CardType, CardID, FieldName, Message<br>• 규칙 예시: GRID 좌표 수치형 검증, 요소의 노드 ID → Grid 존재 여부 확인, Property ID → Property 존재 여부 확인, Material ID → Material 존재 여부 확인<br>• 규칙 클래스 분리: `GridRule`, `ElementRule`, `PropertyRule`, `MaterialRule`, `LoadRule`, `BcRule`<br>• `BdfValidator`는 규칙 목록을 순회하며 결과 수집 |
| **완료 기준** | • 존재하지 않는 GRID ID 참조 시 Error 생성 확인<br>• 필수 필드 누락 시 Error 생성 확인<br>• ValidationResult 목록 JSON 직렬화 정상 동작 |
| **구현 요구사항 ID** | `R004`, `R006` |

---

### JsonExporter

> **구현 요구사항:** `R005`, `R006` | **변경 유형:** 신규 구현

| 항목 | 내용 |
|------|------|
| **역할** | BdfModel → JSON 파일 출력, ValidationResult 목록 → 별도 JSON 파일 출력 |
| **현재 문제** | 미구현 |
| **개선 내용** | • `System.Text.Json` 사용 (들여쓰기 포함 옵션)<br>• 출력 구조: `{ "grids": [...], "elements": [...], "properties": [...], "materials": [...], "loads": [...], "boundaryConditions": [...], "caseControl": {...} }`<br>• 검증 결과는 `<파일명>_validation.json`으로 별도 저장<br>• 출력 경로: 입력 BDF와 동일 폴더 또는 `--output` 옵션 경로 |
| **완료 기준** | • 출력 JSON이 유효한 JSON 형식<br>• 모든 카드 타입이 올바른 배열에 포함됨<br>• ValidationResult JSON에 severity, cardType, message 필드 존재 |
| **구현 요구사항 ID** | `R005`, `R006` |

---

### NastranRunner

> **구현 요구사항:** `R007` | **변경 유형:** 신규 구현

| 항목 | 내용 |
|------|------|
| **역할** | 외부 Nastran 실행 파일을 CLI로 호출하고 F06 파일 생성을 대기하는 프로세스 래퍼 |
| **현재 문제** | 미구현 |
| **개선 내용** | • `Process.Start()`로 Nastran 실행, stdout/stderr 리다이렉트<br>• Nastran 실행 파일 경로는 환경변수 또는 `--nastran-exe` 옵션으로 주입<br>• F06 파일 생성 대기 (타임아웃 설정 가능)<br>• 실행 실패 시 명확한 오류 메시지 출력 |
| **완료 기준** | • `--nastran` 옵션 없을 시 NastranRunner 미실행 확인<br>• Nastran 실행 파일 경로 미설정 시 사용자 안내 메시지 출력<br>• F06 파일 경로 반환 확인 |
| **구현 요구사항 ID** | `R007` |

---

### F06Parser

> **구현 요구사항:** `R008` | **변경 유형:** 신규 구현

| 항목 | 내용 |
|------|------|
| **역할** | Nastran F06 결과 파일에서 FATAL, WARNING, USER WARNING 키워드 기반으로 메시지 추출 및 구조화 |
| **현재 문제** | 미구현 |
| **개선 내용** | • `FATAL`, `WARNING`, `USER WARNING` 패턴으로 라인 스캔<br>• 메시지 컨텍스트(전후 라인) 포함 수집<br>• `F06Result`: `List<F06Message>` 반환<br>• `F06Message`: Level(Fatal/Warning), LineNumber, Message<br>• 결과 JSON 출력: `<파일명>_f06_summary.json` |
| **완료 기준** | • FATAL 포함 F06에서 Fatal 메시지 추출 확인<br>• WARNING 포함 F06에서 Warning 메시지 추출 확인<br>• F06 파일 없을 시 명확한 오류 처리 |
| **구현 요구사항 ID** | `R008` |

---

### Program

> **구현 요구사항:** `R009`, `R011` | **변경 유형:** 신규 구현

| 항목 | 내용 |
|------|------|
| **역할** | CLI 진입점. 인자 파싱 후 BdfParser → BdfValidator → JsonExporter → (선택) NastranRunner → F06Parser 파이프라인 오케스트레이션 |
| **현재 문제** | 미구현 |
| **개선 내용** | • 필수 인자: `<bdf파일 경로>`<br>• 옵션: `--nastran`(Nastran 실행 활성화), `--nastran-exe <경로>`, `--output <출력폴더>`<br>• `--help` 옵션으로 사용법 출력<br>• 파일 미존재, 확장자 오류 시 명확한 오류 메시지 출력 후 종료 |
| **완료 기준** | • `dotnet run -- model.bdf` 실행 시 JSON 파일 2개(모델, 검증결과) 생성 확인<br>• `--nastran` 옵션 시 F06 요약 JSON 추가 생성 확인<br>• 잘못된 경로 입력 시 exit code 1 및 오류 메시지 출력 |
| **구현 요구사항 ID** | `R009`, `R011` |

---

## 🔗 데이터/인터페이스 변경 사항

### 핵심 인터페이스 정의

| 대상 | 변경 유형 | 변경 내용 | 하위 호환성 |
|------|-----------|-----------|------------|
| `IElement` | 신규 | `int Id`, `int PropertyId`, `int[] NodeIds`, `string CardType` | 해당 없음 |
| `IProperty` | 신규 | `int Id`, `string CardType` | 해당 없음 |
| `IMaterial` | 신규 | `int Id`, `string CardType` | 해당 없음 |
| `ILoad` | 신규 | `int Id`, `int SubcaseId`, `string CardType` | 해당 없음 |
| `IBoundaryCondition` | 신규 | `int Id`, `string CardType` | 해당 없음 |
| `ValidationResult` | 신규 | `Severity`, `CardType`, `CardId`, `FieldName`, `Message` | 해당 없음 |

### 하위 호환성 영향

- **해당 없음**: 신규 개발로 기존 인터페이스 없음

---

## 🛠️ 기술 제약 조건

### 언어 / 프레임워크

- **C# 12 / .NET 8** — `System.Text.Json` 내장 JSON 직렬화 사용 (Newtonsoft.Json 불필요)
- **xUnit** — 단위 테스트 프레임워크 (FemScanner.Tests 프로젝트)

### 변경 불가 외부 의존성

- **MSC Nastran 실행 파일** — 외부 라이선스 도구, 경로만 주입 방식으로 연동
- **.NET 8 BCL** — 플랫폼 표준 라이브러리

### 유지해야 하는 인터페이스 계약

- **CLI 호출 형식**: `dotnet run --project FemScanner -- <path/to/model.bdf>` — CLAUDE.md 기준 고정
- **JSON 출력 키 이름**: `grids`, `elements`, `properties`, `materials`, `loads`, `boundaryConditions`, `caseControl` — 하위 호환성 기준점
