# FemScanner 개발 로드맵

MSC Nastran BDF 파일을 파싱, 검증, JSON 추출하는 CLI 도구를 단계적으로 신규 개발한다.

## 개요

FemScanner는 MSC Nastran BDF 파일의 수동 분석 문제를 해결하기 위해 신규 개발하는 C# Console 도구(.NET 8)이다. 현재 C# 소스 파일이 없는 상태에서 다음 기능을 구현한다:

- **BDF 파싱 (R001)**: fixed-field(8자/16자), free-field(콤마), continuation 카드 완전 지원
- **Case Control 파싱 (R002)**: BEGIN BULK 이전 섹션 분리 및 SUBCASE, LOAD, SPC 등 파싱
- **카드 모델화 (R003)**: GRID, Element 7종, Property 5종, Material 3종, Load 4종, BC 3종 모델 클래스
- **문법 검증 (R004)**: 카드별 필드 수/타입/참조 무결성 규칙 검사 및 오류 리포트
- **JSON 출력 (R005, R006)**: 구조화된 JSON 및 검증 결과 파일 출력
- **Nastran 연동 (R007, R008)**: 외부 Nastran 실행 및 F06 결과 파싱 (옵션)
- **CLI 인터페이스 (R009)**: dotnet run -- <bdf파일> 형식의 명령줄 도구
- **품질 (R010, R011)**: 단위 테스트 및 에러 핸들링

## 개발 워크플로우

1. **작업 계획**

   - 설계 문서(CLAUDE.md, PRD.md)를 분석하고 현재 상태를 정확히 파악
   - 새로운 작업을 포함하도록 `ROADMAP.md` 업데이트
   - 우선순위 작업은 마지막 완료된 작업 다음에 삽입

2. **작업 생성**

   - 설계 문서를 학습하고 현재 상태를 파악
   - `/tasks` 디렉토리에 새 작업 파일 생성
   - 명명 형식: `XXX-description.md` (예: `001-project-setup.md`)
   - 고수준 명세서, 대상 파일, 수락 기준, 구현 단계 포함
   - **모든 작업에 "## 검증 체크리스트" 섹션 필수 포함 (단위 테스트 통과 + 회귀 검증 시나리오)**
   - 예시를 위해 `/tasks` 디렉토리의 마지막 완료된 작업 참조

3. **작업 구현**

   - 작업 파일의 명세서를 따름
   - 각 단계 후 작업 파일 내 진행 상황 업데이트
   - 구현 완료 후 단위 테스트 전부 통과 확인
   - 테스트 통과 확인 후 다음 단계로 진행
   - 각 단계 완료 후 중단하고 추가 지시를 기다림

4. **로드맵 업데이트**

   - 로드맵에서 완료된 작업을 표시

## 요구사항 추적표

| 요구사항 ID | 설명 | 관련 Task | Phase |
|-------------|------|-----------|-------|
| R001 | BDF 포맷 파싱 (fixed/free/large-field + continuation) | Task 003, 004 | Phase 1, 2 |
| R002 | Case Control 파싱 | Task 004, 009 | Phase 2 |
| R003 | Bulk Data 카드 모델화 | Task 005, 006, 007, 008, 009 | Phase 1, 2 |
| R004 | Nastran 문법 검증 | Task 011, 012 | Phase 3 |
| R005 | JSON 출력 | Task 013 | Phase 3 |
| R006 | ValidationResult 출력 | Task 011, 013 | Phase 3 |
| R007 | Nastran 실행 (옵션) | Task 016 | Phase 4 |
| R008 | F06 결과 파싱 | Task 017 | Phase 4 |
| R009 | CLI 인터페이스 | Task 014 | Phase 3 |
| R010 | 단위 테스트 | Task 003, 004, 008, 012, 015, 019 | Phase 1, 2, 3, 5 |
| R011 | 에러 핸들링 | Task 014, 018 | Phase 3, 4 |

---

## 개발 단계

### Phase 1: 기반 구조 -- 프로젝트 셋업 및 핵심 파싱 인프라

> 목표: 프로젝트 골격을 세우고, BDF 파일의 라인 단위 토큰 분리(CardReader) 및 기반 모델 인터페이스를 확보한다.

- [x] ✅ **Task 001: 프로젝트 스캐폴딩 및 솔루션 구성** -- 우선순위
  - [x] FemScanner 콘솔 프로젝트 생성 (.NET 8, C# 12)
  - [x] FemScanner.Tests xUnit 테스트 프로젝트 생성
  - [x] 솔루션 파일(.sln) 생성 및 프로젝트 참조 구성
  - [x] CLAUDE.md 기준 디렉토리 구조 생성 (Parsers/, Models/, Validators/, Exporters/)
  - [x] `dotnet build` 및 `dotnet test` 정상 동작 확인
  - 요구사항: 없음 (인프라)

- [x] ✅ **Task 002: 기반 인터페이스 및 모델 컨테이너 정의** -- 우선순위
  - [x] `IElement`, `IProperty`, `IMaterial`, `ILoad`, `IBoundaryCondition` 인터페이스 정의
  - [x] `ValidationResult` 클래스 정의: Severity(Error/Warning), CardType, CardId, FieldName, Message
  - [x] `Models/BdfModel.cs` 신규 생성 (전체 컨테이너, 각 인터페이스별 List 프로퍼티)
  - [x] JSON 직렬화 가능 여부 기본 확인
  - 요구사항: R003

- [x] ✅ **Task 003: CardReader 구현 (fixed/free/large-field + continuation) 및 단위 테스트**
  - [x] `Parsers/CardReader.cs` 신규 생성
  - [x] 8자 고정폭 슬라이싱으로 fixed-field 토큰 추출 (최대 10필드)
  - [x] `$` 시작 라인 주석 스킵, 빈 라인 처리
  - [x] 콤마 포함 시 free-field 분리(Split(',')) 처리
  - [x] `*` 접두어 감지 시 16자 large-field 모드 전환
  - [x] `+` 또는 공백 두 번째 필드 감지로 continuation 카드 병합
  - [x] 각 필드의 앞뒤 공백 Trim 처리
  - [x] CardReader 단위 테스트 작성: fixed 8자, free-field, large-field 16자, continuation 병합 각각 검증
  - [x] 테스트 샘플 BDF 데이터 fixtures 준비
  - 요구사항: R001, R010

### Phase 2: 핵심 기능 -- BdfParser, 카드 모델, CaseControl

> 목표: BDF 파일 전체 파싱 파이프라인을 완성한다. 모든 카드 타입 모델을 구현하고, BdfParser를 통해 파싱 -> BdfModel 집적까지 동작하도록 한다.

- [x] ✅ **Task 004: BdfParser 핵심 구현 (섹션 분리 + GRID 파싱)**
  - [x] `Parsers/BdfParser.cs` 신규 생성
  - [x] `BEGIN BULK` / `ENDDATA` 구분자로 Case Control / Bulk Data 섹션 분리
  - [x] `Models/Grids/Grid.cs` 구현 (ID, X, Y, Z, CoordID, OutCoordID)
  - [x] GRID 카드 파싱 로직 구현 (CardReader 활용)
  - [x] 미지원 카드 경고 수집 후 스킵 처리
  - [x] BdfParser 단위 테스트: 섹션 분리 정확성, GRID 파싱 정확성 검증
  - 요구사항: R001, R002, R010

- [x] ✅ **Task 005: Element 모델 클래스 구현 (7종)**
  - [x] `CQUAD4` (EID, PID, 4노드), `CTRIA3` (EID, PID, 3노드) 구현
  - [x] `CTETRA` (EID, PID, 4노드), `CHEXA` (EID, PID, 8노드) 구현
  - [x] `CBAR` (EID, PID, 2노드, 방향벡터), `CBEAM` (EID, PID, 2노드, 방향벡터) 구현
  - [x] `CROD` (EID, PID, 2노드) 구현
  - [x] 모든 Element가 `IElement` 인터페이스 구현 확인
  - [x] BdfParser에 Element 카드 디스패치 추가
  - 요구사항: R003

- [x] ✅ **Task 006: Property 및 Material 모델 클래스 구현**
  - [x] Properties: `PSHELL`, `PSOLID`, `PBAR`, `PBEAM`, `PROD` 구현 (각각 `IProperty`)
  - [x] Materials: `MAT1`, `MAT2`, `MAT8` 구현 (각각 `IMaterial`)
  - [x] BdfParser에 Property/Material 카드 디스패치 추가
  - 요구사항: R003

- [x] ✅ **Task 007: Load 및 BoundaryCondition 모델 클래스 구현**
  - [x] Loads: `FORCE`, `MOMENT`, `PLOAD`, `PLOAD4` 구현 (각각 `ILoad`)
  - [x] BCs: `SPC`, `SPC1`, `MPC` 구현 (각각 `IBoundaryCondition`)
  - [x] BdfParser에 Load/BC 카드 디스패치 추가
  - 요구사항: R003

- [x] ✅ **Task 008: 카드 모델 파싱 통합 테스트**
  - [x] 모든 카드 타입 포함 샘플 BDF 파일 작성
  - [x] GRID + Element + Property + Material + Load + BC 혼합 파싱 통합 테스트
  - [x] 파싱 결과 BdfModel 내 각 List 정확성 검증
  - [x] 미지원 카드 경고 메시지에 카드명 및 라인 번호 포함 확인
  - 요구사항: R003, R010

- [x] ✅ **Task 009: CaseControl 섹션 파싱 구현**
  - [x] `Models/CaseControl.cs` 신규 생성
  - [x] `키워드 = 값` 및 `키워드(옵션) = 값` 패턴 파싱
  - [x] SUBCASE 블록별 계층 구조 구성
  - [x] SUBCASE ID, LOAD, SPC, METHOD, DISP, STRESS 지시문 추출
  - [x] BdfParser에서 BEGIN BULK 이전 섹션을 CaseControl 파서로 위임
  - [x] CaseControl 파싱 단위 테스트 작성
  - 요구사항: R002

- [x] ✅ **Task 010: BdfParser 디스패치 완성 및 통합 정리**
  - [x] 카드명 기반 switch/dictionary 디스패치 매핑 최종 정리
  - [x] 모든 지원 카드 타입 디스패치 누락 없이 매핑 확인
  - [x] 파싱 결과를 BdfModel에 완전 집적
  - [x] BdfParser 전체 통합 테스트: 모든 카드 타입 + CaseControl 포함 BDF 파싱 검증
  - 요구사항: R001, R002, R003

### Phase 3: 검증 및 출력 -- Validator, JsonExporter, CLI

> 목표: 파싱된 모델에 대한 문법 검증, JSON 직렬화 출력, CLI 진입점을 구현하여 핵심 파이프라인(파싱 -> 검증 -> 출력)을 완성한다.

- [x] ✅ **Task 011: BdfValidator 기반 및 GridRule 구현**
  - [x] `Validators/BdfValidator.cs` 신규 생성 (검증 오케스트레이터)
  - [x] 규칙 인터페이스 `IValidationRule` 정의
  - [x] `GridRule` 구현: 좌표 수치형 검증, 필수 필드(ID, X, Y, Z) 존재 확인
  - [x] BdfValidator가 규칙 목록을 순회하며 ValidationResult 수집
  - [x] GridRule 단위 테스트: 정상 GRID 및 오류 GRID 검증
  - 요구사항: R004, R006

- [x] ✅ **Task 012: 카드별 검증 규칙 구현 및 참조 무결성 검증**
  - [x] `ElementRule`: 노드 ID -> Grid 존재 여부, PropertyId 참조 확인
  - [x] `PropertyRule`: MaterialId 참조 확인, 필수 필드 검증
  - [x] `MaterialRule`: 물성값 수치형 검증, 필수 필드 검증
  - [x] `LoadRule`: 노드/요소 참조 무결성 확인
  - [x] `BcRule`: 노드 참조 무결성 확인
  - [x] 검증 규칙 단위 테스트: 정상 케이스 및 오류 케이스(미존재 참조, 필수 필드 누락) 검증
  - 요구사항: R004, R006, R010

- [x] ✅ **Task 013: JsonExporter 구현 (모델 및 검증 결과 JSON 출력)**
  - [x] `Exporters/JsonExporter.cs` 신규 생성
  - [x] `System.Text.Json` 사용, 들여쓰기 포함 옵션
  - [x] BdfModel JSON 출력 구조: `{ "grids": [...], "elements": [...], "properties": [...], "materials": [...], "loads": [...], "boundaryConditions": [...], "caseControl": {...} }`
  - [x] `[JsonPolymorphic]` 또는 커스텀 JsonConverter로 다형성 직렬화 처리
  - [x] ValidationResult 목록을 `<파일명>_validation.json`으로 별도 저장
  - [x] 출력 경로: 입력 BDF와 동일 폴더 또는 `--output` 옵션 경로
  - [x] 출력 JSON 유효성 검증 테스트
  - 요구사항: R005, R006

- [x] ✅ **Task 014: Program CLI 진입점 구현**
  - [x] `Program.cs` 신규 생성
  - [x] 필수 인자: `<bdf파일 경로>`, 옵션: `--nastran`, `--nastran-exe <경로>`, `--output <출력폴더>`, `--help`
  - [x] BdfParser -> BdfValidator -> JsonExporter 파이프라인 오케스트레이션
  - [x] 파일 미존재, 확장자 오류 시 명확한 오류 메시지 출력 후 exit code 1 종료
  - [x] 빈 파일, 잘못된 포맷 등 경계 조건 에러 핸들링
  - [x] 콘솔에 ValidationResult 요약 출력 (Error N건, Warning N건)
  - 요구사항: R009, R011

- [x] ✅ **Task 015: 핵심 파이프라인 End-to-End 통합 테스트**
  - [x] 샘플 BDF 파일(GRID + Element + Property + Material + Load + BC + CaseControl 포함) 준비
  - [x] 파싱 -> 검증 -> JSON 출력 전체 파이프라인 end-to-end 테스트
  - [x] 출력 JSON 파일이 유효한 JSON 형식인지 검증
  - [x] 모든 카드 타입이 올바른 배열에 포함되는지 검증
  - [x] ValidationResult JSON에 severity, cardType, message 필드 존재 확인
  - [x] 오류 BDF (미존재 참조 포함) 입력 시 검증 결과 정확성 확인
  - 요구사항: R010

### Phase 4: 옵션 기능 -- Nastran 연동 및 F06 파싱

> 목표: 선택적 Nastran 실행 및 F06 결과 파싱 기능을 추가하여 해석 자동화 파이프라인을 완성한다.

- [x] ✅ **Task 016: NastranRunner 구현 (외부 프로세스 실행)**
  - [x] `NastranRunner.cs` 신규 생성
  - [x] `Process.Start()`로 Nastran 실행, stdout/stderr 리다이렉트
  - [x] Nastran 실행 파일 경로: 환경변수 `NASTRAN_EXE` 또는 `--nastran-exe` 옵션으로 주입
  - [x] F06 파일 생성 대기 (타임아웃 설정 가능, 기본 300초)
  - [x] 실행 실패 시 명확한 오류 메시지 출력
  - [x] `--nastran` 옵션 없을 시 NastranRunner 미실행 확인
  - 요구사항: R007

- [x] ✅ **Task 017: F06Parser 구현 (F06 결과 파싱)**
  - [x] `Parsers/F06Parser.cs` 신규 생성
  - [x] `FATAL`, `WARNING`, `USER WARNING` 패턴으로 라인 스캔
  - [x] 메시지 컨텍스트(전후 2라인) 포함 수집
  - [x] `F06Result`, `F06Message` 모델 구현 (Level: Fatal/Warning, LineNumber, Message)
  - [x] 결과 JSON 출력: `<파일명>_f06_summary.json`
  - [x] F06 파일 없을 시 명확한 오류 처리
  - [x] F06Parser 단위 테스트: FATAL/WARNING 추출 정확성 검증
  - 요구사항: R008

- [x] ✅ **Task 018: Program에 Nastran 연동 파이프라인 통합**
  - [x] `--nastran` 옵션 시 NastranRunner -> F06Parser 파이프라인 추가
  - [x] F06 요약 JSON 추가 생성 확인
  - [x] Nastran 미설치 환경에서의 우아한 실패 처리
  - [x] `--help` 출력에 Nastran 관련 옵션 설명 포함
  - 요구사항: R007, R008, R009, R011

### Phase 5: 테스트 보강 및 품질 마무리

> 목표: 코드 품질을 최종 점검하고, 테스트 커버리지를 보강하며, 개발 결과를 문서화한다.

- [x] ✅ **Task 019: 코드 품질 최종 점검 및 정리**
  - [x] 정적 분석(dotnet analyzers) 실행 및 경고 해소
  - [x] 네이밍 컨벤션 일관성 검토 (C# 표준 PascalCase)
  - [x] 불필요한 using문, 데드코드 제거
  - [x] XML 문서 주석 정비 (public API에 summary 태그)
  - [x] 전체 테스트 스위트 통과 최종 확인 (73 tests passed)
  - 요구사항: R010

- [x] ✅ **Task 020: 개발 결과 문서화**
  - [x] 지원 카드 목록 및 제한사항 정리
  - [x] CLI 사용법 상세 문서 작성 (README.md 업데이트)
  - [x] JSON 출력 스키마 예시 문서화
  - [x] 다음 단계 개발 후보 목록 작성 (시각화, BDF 편집, MAT8 완전 지원 등)

---

## 완료 기준 체크리스트

### 필수 기능 (MVP)

- [x] `dotnet run -- model.bdf` 실행 시 JSON 파일 2개(모델, 검증결과) 생성 (R005, R006, R009)
- [x] fixed-field 8자, free-field 콤마, large-field 16자 파싱 정확도 100% (R001)
- [x] continuation 카드 병합 후 단일 토큰 배열 반환 (R001)
- [x] BEGIN BULK 이전/이후 섹션 정확히 분리 (R002)
- [x] SUBCASE ID, LOAD ID, SPC ID 정확히 추출 (R002)
- [x] GRID, CQUAD4, PSHELL, MAT1, FORCE, SPC 각 1개 이상 파싱 성공 (R003)
- [x] 모든 카드 모델이 JSON 직렬화 가능 (R003, R005)
- [x] 존재하지 않는 GRID ID 참조 시 Error 생성 (R004)
- [x] 필수 필드 누락 시 Error 생성 (R004)
- [x] 출력 JSON이 유효한 JSON 형식 (R005)
- [x] ValidationResult JSON에 severity, cardType, message 필드 존재 (R006)
- [x] 잘못된 경로 입력 시 exit code 1 및 오류 메시지 출력 (R011)

### 옵션 기능

- [x] `--nastran` 옵션 시 Nastran 실행 및 F06 요약 JSON 생성 (R007, R008)
- [x] Nastran 실행 파일 경로 미설정 시 사용자 안내 메시지 출력 (R007)
- [x] FATAL 포함 F06에서 Fatal 메시지 추출 (R008)
- [x] WARNING 포함 F06에서 Warning 메시지 추출 (R008)

### 품질 기준

- [x] 파서, 검증기, 익스포터 주요 경로 단위 테스트 존재 (R010) — 73 tests
- [x] `dotnet build` 경고 0건
- [x] `dotnet test` 전체 통과
- [x] public API에 XML 문서 주석 존재
