# FemScanner 개발 로드맵

MSC Nastran BDF 파일을 파싱, 검증, JSON 추출하는 CLI 도구를 안전 우선 접근법(Safety-First Approach)으로 단계적 신규 개발한다.

## 개요

FemScanner는 MSC Nastran BDF 파일의 수동 분석 문제를 해결하기 위해 다음 기능을 신규 개발한다:

- **BDF 파싱**: fixed-field(8자/16자), free-field(콤마), continuation 카드 완전 지원
- **문법 검증**: 카드별 필드 수/타입/참조 무결성 규칙 검사 및 오류 리포트
- **JSON 추출**: 카드 타입별 구조화된 JSON 출력 (grids, elements, properties, materials, loads, boundaryConditions, caseControl)
- **Nastran 연동 (옵션)**: 외부 Nastran 실행 및 F06 결과 파싱

## 개발 워크플로우

1. **작업 계획**

   - 기존 설계 문서(CLAUDE.md, PRD.md)를 분석하고 현재 상태를 정확히 파악
   - 새로운 작업을 포함하도록 `ROADMAP.md` 업데이트
   - 우선순위 작업은 마지막 완료된 작업 다음에 삽입

2. **작업 생성**

   - 기존 설계 문서를 학습하고 현재 상태를 파악
   - `/tasks` 디렉토리에 새 작업 파일 생성
   - 명명 형식: `XXX-description.md` (예: `001-project-setup.md`)
   - 고수준 명세서, 대상 파일, 수락 기준, 구현 단계 포함
   - **변경 작업 시 "## 검증 체크리스트" 섹션 필수 포함 (단위 테스트 통과 + 회귀 검증 시나리오)**
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

| 요구사항 ID | 설명 | 관련 Task |
|-------------|------|-----------|
| R001 | BDF 포맷 파싱 (fixed/free/large-field + continuation) | Task 002, 003 |
| R002 | Case Control 파싱 | Task 005 |
| R003 | Bulk Data 카드 모델화 | Task 004, 006, 007, 008 |
| R004 | Nastran 문법 검증 | Task 011, 012 |
| R005 | JSON 출력 | Task 013 |
| R006 | ValidationResult 출력 | Task 012, 013 |
| R007 | Nastran 실행 (옵션) | Task 016 |
| R008 | F06 결과 파싱 | Task 017 |
| R009 | CLI 인터페이스 | Task 014 |
| R010 | 단위 테스트 | Task 003, 005, 008, 012, 015 |
| R011 | 에러 핸들링 | Task 014, 018 |

---

## 개선 단계

### Phase 1: 프로젝트 셋업 및 핵심 파싱 기반 구축

- **Task 001: 프로젝트 스캐폴딩 및 솔루션 구성** - 우선순위
  - FemScanner 콘솔 프로젝트 생성 (.NET 8, C# 12)
  - FemScanner.Tests xUnit 테스트 프로젝트 생성
  - 솔루션 파일(.sln) 생성 및 프로젝트 참조 구성
  - CLAUDE.md 기준 디렉토리 구조 생성 (Parsers/, Models/, Validators/, Exporters/)
  - `dotnet build` 및 `dotnet test` 정상 동작 확인

- **Task 002: CardReader 핵심 구현 (fixed-field 8자 파싱)** - 우선순위
  - `Parsers/CardReader.cs` 신규 생성
  - 8자 고정폭 슬라이싱으로 fixed-field 토큰 추출 (최대 10필드)
  - `$` 시작 라인 주석 스킵 처리
  - 빈 라인 및 공백 라인 처리
  - 각 필드의 앞뒤 공백 Trim 처리
  - 요구사항: R001

- **Task 003: CardReader 확장 (free-field, large-field, continuation) 및 단위 테스트**
  - 콤마 포함 시 free-field 분리(Split(',')) 처리
  - `*` 접두어 감지 시 16자 large-field 모드 전환
  - `+` 또는 공백 두 번째 필드 감지로 continuation 카드 병합
  - CardReader 전체 단위 테스트 작성: fixed 8자, free-field, large-field 16자, continuation 병합 각각 검증
  - 테스트 샘플 BDF 데이터 fixtures 준비
  - 요구사항: R001, R010

- **Task 004: 기반 인터페이스 및 Grid 모델 정의**
  - `Models/BdfModel.cs` 신규 생성 (전체 컨테이너)
  - `IElement`, `IProperty`, `IMaterial`, `ILoad`, `IBoundaryCondition` 인터페이스 정의
  - `Models/Grids/Grid.cs` 구현 (ID, X, Y, Z, CoordID, OutCoordID)
  - BdfModel에 `List<Grid>` 및 각 인터페이스별 List 프로퍼티 추가
  - 요구사항: R003

- **Task 005: BdfParser 핵심 구현 (섹션 분리 + GRID 파싱)**
  - `Parsers/BdfParser.cs` 신규 생성
  - `BEGIN BULK` / `ENDDATA` 구분자로 Case Control / Bulk Data 섹션 분리
  - GRID 카드 파싱 로직 구현 (CardReader 활용)
  - 미지원 카드 경고 수집 후 스킵 처리
  - BdfParser 단위 테스트: 섹션 분리 정확성, GRID 파싱 정확성 검증
  - 요구사항: R001, R002, R010

### Phase 2: 모델 완성 및 검증

- **Task 006: Element 모델 클래스 구현 (7종)**
  - `CQUAD4` (EID, PID, 4노드), `CTRIA3` (EID, PID, 3노드) 구현
  - `CTETRA` (EID, PID, 4노드), `CHEXA` (EID, PID, 8노드) 구현
  - `CBAR` (EID, PID, 2노드, 방향벡터), `CBEAM` (EID, PID, 2노드, 방향벡터) 구현
  - `CROD` (EID, PID, 2노드) 구현
  - 모든 Element가 `IElement` 인터페이스 구현 확인
  - BdfParser에 Element 카드 디스패치 추가
  - 요구사항: R003

- **Task 007: Property 및 Material 모델 클래스 구현**
  - Properties: `PSHELL`, `PSOLID`, `PBAR`, `PBEAM`, `PROD` 구현 (각각 `IProperty` 구현)
  - Materials: `MAT1`, `MAT2`, `MAT8` 구현 (각각 `IMaterial` 구현)
  - BdfParser에 Property/Material 카드 디스패치 추가
  - 요구사항: R003

- **Task 008: Load 및 BoundaryCondition 모델 클래스 구현**
  - Loads: `FORCE`, `MOMENT`, `PLOAD`, `PLOAD4` 구현 (각각 `ILoad` 구현)
  - BCs: `SPC`, `SPC1`, `MPC` 구현 (각각 `IBoundaryCondition` 구현)
  - BdfParser에 Load/BC 카드 디스패치 추가
  - 모든 카드 모델 파싱 통합 테스트 작성 (GRID + Element + Property + Material + Load + BC 혼합 BDF)
  - 요구사항: R003, R010

- **Task 009: CaseControl 섹션 파싱 구현**
  - `Models/CaseControl.cs` 신규 생성
  - `키워드 = 값` 및 `키워드(옵션) = 값` 패턴 파싱
  - SUBCASE 블록별 계층 구조 구성
  - SUBCASE ID, LOAD, SPC, METHOD, DISP, STRESS 지시문 추출
  - BdfParser에서 BEGIN BULK 이전 섹션을 CaseControl 파서로 위임
  - 요구사항: R002

- **Task 010: BdfParser 통합 및 디스패치 완성**
  - 카드명 기반 switch/dictionary 디스패치 정리 및 최적화
  - 모든 지원 카드 타입에 대한 디스패치 매핑 완성
  - 미지원 카드 경고 메시지에 카드명 및 라인 번호 포함
  - 파싱 결과를 BdfModel에 완전 집적
  - BdfParser 통합 테스트: 모든 카드 타입 포함 BDF 파일 파싱 검증
  - 요구사항: R001, R002, R003

- **Task 011: ValidationResult 모델 및 BdfValidator 기반 구현**
  - `ValidationResult` 클래스 구현: Severity(Error/Warning), CardType, CardId, FieldName, Message
  - `Validators/BdfValidator.cs` 신규 생성 (검증 오케스트레이터)
  - 규칙 인터페이스 `IValidationRule` 정의
  - `GridRule` 구현: 좌표 수치형 검증, 필수 필드(ID, X, Y, Z) 존재 확인
  - BdfValidator가 규칙 목록을 순회하며 ValidationResult 수집
  - 요구사항: R004, R006

- **Task 012: 카드별 검증 규칙 구현 및 참조 무결성 검증**
  - `ElementRule`: 노드 ID가 Grid에 존재하는지 참조 무결성 확인, PropertyId 참조 확인
  - `PropertyRule`: MaterialId 참조 확인, 필수 필드 검증
  - `MaterialRule`: 물성값 수치형 검증, 필수 필드 검증
  - `LoadRule`: 노드/요소 참조 무결성 확인
  - `BcRule`: 노드 참조 무결성 확인
  - 검증 규칙 단위 테스트: 정상 케이스 및 오류 케이스(미존재 참조, 필수 필드 누락) 검증
  - 요구사항: R004, R006, R010

### Phase 3: 출력 및 통합

- **Task 013: JsonExporter 구현 (모델 및 검증 결과 JSON 출력)**
  - `Exporters/JsonExporter.cs` 신규 생성
  - `System.Text.Json` 사용, 들여쓰기 포함 옵션
  - BdfModel JSON 출력 구조: `{ "grids": [...], "elements": [...], "properties": [...], "materials": [...], "loads": [...], "boundaryConditions": [...], "caseControl": {...} }`
  - `[JsonPolymorphic]` 또는 커스텀 JsonConverter로 IElement/IProperty/IMaterial/ILoad/IBoundaryCondition 다형성 직렬화
  - ValidationResult 목록을 `<파일명>_validation.json`으로 별도 저장
  - 출력 경로: 입력 BDF와 동일 폴더 또는 `--output` 옵션 경로
  - 요구사항: R005, R006

- **Task 014: Program CLI 진입점 구현**
  - `Program.cs` 신규 생성
  - 필수 인자: `<bdf파일 경로>`, 옵션: `--nastran`, `--nastran-exe <경로>`, `--output <출력폴더>`, `--help`
  - BdfParser -> BdfValidator -> JsonExporter 파이프라인 오케스트레이션
  - 파일 미존재, 확장자 오류 시 명확한 오류 메시지 출력 후 exit code 1 종료
  - 빈 파일, 잘못된 포맷 등 경계 조건 에러 핸들링
  - 콘솔에 ValidationResult 요약 출력 (Error N건, Warning N건)
  - 요구사항: R009, R011

- **Task 015: 전체 파이프라인 통합 테스트**
  - 샘플 BDF 파일(GRID + Element + Property + Material + Load + BC + CaseControl 포함) 준비
  - 파싱 -> 검증 -> JSON 출력 전체 파이프라인 end-to-end 테스트
  - 출력 JSON 파일이 유효한 JSON 형식인지 검증
  - 모든 카드 타입이 올바른 배열에 포함되는지 검증
  - ValidationResult JSON에 severity, cardType, message 필드 존재 확인
  - 오류 BDF (미존재 참조 포함) 입력 시 검증 결과 정확성 확인
  - 요구사항: R010

### Phase 4: 옵션 기능 및 마무리

- **Task 016: NastranRunner 구현 (외부 프로세스 실행)**
  - `NastranRunner.cs` 신규 생성
  - `Process.Start()`로 Nastran 실행, stdout/stderr 리다이렉트
  - Nastran 실행 파일 경로는 환경변수 `NASTRAN_EXE` 또는 `--nastran-exe` 옵션으로 주입
  - F06 파일 생성 대기 (타임아웃 설정 가능, 기본 300초)
  - 실행 실패 시 명확한 오류 메시지 출력
  - `--nastran` 옵션 없을 시 NastranRunner 미실행 확인
  - 요구사항: R007

- **Task 017: F06Parser 구현 (F06 결과 파싱)**
  - `Parsers/F06Parser.cs` 신규 생성
  - `FATAL`, `WARNING`, `USER WARNING` 패턴으로 라인 스캔
  - 메시지 컨텍스트(전후 2라인) 포함 수집
  - `F06Result`, `F06Message` 모델 구현 (Level: Fatal/Warning, LineNumber, Message)
  - 결과 JSON 출력: `<파일명>_f06_summary.json`
  - F06 파일 없을 시 명확한 오류 처리
  - F06Parser 단위 테스트: FATAL/WARNING 추출 정확성 검증
  - 요구사항: R008

- **Task 018: Program에 Nastran 연동 파이프라인 통합**
  - `--nastran` 옵션 시 NastranRunner -> F06Parser 파이프라인 추가
  - F06 요약 JSON 추가 생성 확인
  - Nastran 미설치 환경에서의 우아한 실패 처리
  - `--help` 출력에 Nastran 관련 옵션 설명 포함
  - 요구사항: R007, R008, R009, R011

- **Task 019: 코드 품질 최종 점검 및 정리**
  - 정적 분석(dotnet analyzers) 실행 및 경고 해소
  - 네이밍 컨벤션 일관성 검토 (C# 표준 PascalCase)
  - 불필요한 using문, 데드코드 제거
  - XML 문서 주석 정비 (public API에 summary 태그)
  - 전체 테스트 스위트 통과 최종 확인

- **Task 020: 개발 결과 문서화**
  - 지원 카드 목록 및 제한사항 정리
  - CLI 사용법 상세 문서 작성 (README.md)
  - JSON 출력 스키마 예시 문서화
  - 다음 단계 개발 후보 목록 작성 (시각화, BDF 편집, MAT8 완전 지원 등)
