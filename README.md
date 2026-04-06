# FemScanner

MSC Nastran BDF(Bulk Data File) 파싱·검증·JSON 추출 CLI 도구 (.NET 8 / C#)

BDF 파일을 구조화된 JSON으로 변환하고 Nastran 규칙 기반 문법 검증 결과를 리포트합니다.  
선택적으로 Nastran을 실행하여 F06 해석 결과를 자동 파싱합니다.

---

## 요구사항

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MSC Nastran (`--nastran` 옵션 사용 시에만 필요)

## 빌드 및 테스트

```bash
# 빌드
dotnet build FemScanner.sln

# 전체 테스트 (73 tests)
dotnet test FemScanner.Tests/FemScanner.Tests.csproj
```

---

## 사용법

```bash
dotnet run --project FemScanner -- <model.bdf> [옵션]
```

### 옵션

| 옵션 | 설명 |
|------|------|
| `<bdf파일>` | 분석할 BDF 파일 경로 (필수) |
| `--nastran` | Nastran 실행 후 F06 결과 파싱 (`NASTRAN_EXE` 환경변수 또는 PATH 필요) |
| `--help` | 도움말 출력 |

### 실행 예시

```bash
# BDF 파싱 → 검증 → JSON 출력
dotnet run --project FemScanner -- model.bdf

# Nastran 연동 (Nastran 실행 → F06 파싱 포함)
export NASTRAN_EXE=/opt/nastran/bin/nastran
dotnet run --project FemScanner -- model.bdf --nastran
```

### 콘솔 출력 예시

```
파싱 중: model.bdf
  GRID: 120개, Elements: 85개, Properties: 5개, Materials: 3개
검증 중...
  Errors: 1, Warnings: 2
JSON 출력 중: C:\path\to\
  model.json 생성 완료
  model_validation.json 생성 완료
완료.

=== 오류 요약 ===================================================
[BDF 검증] Errors: 1, Warnings: 2
  [Error] CQUAD4 #101 (G1) - 노드 ID 9999에 해당하는 GRID가 존재하지 않습니다.
  [Warning] MAT1 #3 (E) - 탄성계수(E)가 0입니다.
=================================================================
```

---

### 출력 파일

모든 출력 파일은 **BDF 파일과 동일한 폴더**에 생성됩니다.

| 파일명 | 내용 |
|--------|------|
| `<이름>.json` | BDF 모델 전체 (GRID, 요소, 물성, 재질, 하중, 경계조건, Case Control) |
| `<이름>_validation.json` | 검증 결과 목록 (Error/Warning) |
| `<이름>_f06_summary.json` | F06 FATAL/WARNING 메시지 (`--nastran` 옵션 사용 시) |

---

## 지원 카드 목록

| 카테고리 | 지원 카드 |
|----------|-----------|
| Grid | GRID |
| Element | CQUAD4, CTRIA3, CTETRA, CHEXA, CBAR, CBEAM, CROD, CONM2, RBE2 |
| Property | PSHELL, PSOLID, PBAR, PBARL, PBEAM, PBEAML, PROD |
| Material | MAT1, MAT2, MAT8 |
| Load | FORCE, MOMENT, PLOAD, PLOAD4, GRAV |
| Boundary Condition | SPC, SPC1, MPC |
| Case Control | SUBCASE, LOAD, SPC, METHOD, DISP, STRESS 등 |

<details>
<summary>카드별 상세 설명 보기</summary>

### 그리드 (Grids)
| 카드 | 설명 |
|------|------|
| `GRID` | 절점 (ID, CP, X, Y, Z, CD) |

### 요소 (Elements)
| 카드 | 설명 |
|------|------|
| `CQUAD4` | 사각형 쉘 요소 (4노드) |
| `CTRIA3` | 삼각형 쉘 요소 (3노드) |
| `CTETRA` | 사면체 솔리드 요소 (4노드) |
| `CHEXA` | 육면체 솔리드 요소 (8노드) |
| `CBAR` | 보 요소 (2노드 + 방향벡터) |
| `CBEAM` | 보 요소 - 고급 (2노드 + 방향벡터) |
| `CROD` | 봉 요소 (2노드) |

### 물성 (Properties)
| 카드 | 설명 |
|------|------|
| `PSHELL` | 쉘 물성 (MID, 두께) |
| `PSOLID` | 솔리드 물성 (MID) |
| `PBAR` | 보 물성 (MID, A, I1, I2, J) |
| `PBEAM` | 보 물성 - 고급 (MID, A, I1, I2, J) |
| `PROD` | 봉 물성 (MID, A, J) |

### 재질 (Materials)
| 카드 | 설명 |
|------|------|
| `MAT1` | 등방성 재질 (E, G, Nu, Rho) |
| `MAT2` | 이방성 쉘 재질 (G11, G12, G13, G22, G23, G33, Rho) |
| `MAT8` | 직교이방성 재질 (E1, E2, Nu12, G12, G1z, G2z, Rho) |

### 하중 (Loads)
| 카드 | 설명 |
|------|------|
| `FORCE` | 절점 집중력 (SID, G, CID, F, N1, N2, N3) |
| `MOMENT` | 절점 집중 모멘트 (SID, G, CID, M, N1, N2, N3) |
| `PLOAD` | 면압 하중 (SID, P, EID...) |
| `PLOAD4` | 요소 면압 하중 (SID, EID, P) |

### 경계조건 (Boundary Conditions)
| 카드 | 설명 |
|------|------|
| `SPC` | 단일 점 구속 (SID, G, C, D) |
| `SPC1` | 단일 점 구속 - 다중 노드 (SID, C, G...) |
| `MPC` | 다중 점 구속 (SID, G, C, A...) |

### Case Control
SUBCASE, LOAD, SPC, METHOD, DISP, STRESS, FORCE, STRAIN 등의 지시문 파싱 지원

</details>

---

## JSON 출력 스키마

### 모델 JSON (`<이름>.json`)

```json
{
  "grids": [
    { "id": 1, "coordId": 0, "x": 0.0, "y": 0.0, "z": 0.0, "outCoordId": 0 }
  ],
  "elements": [
    { "cardType": "CQUAD4", "id": 1, "propertyId": 1, "nodeIds": [1, 2, 3, 4] }
  ],
  "properties": [
    { "cardType": "PSHELL", "id": 1, "materialId": 1, "thickness": 2.0 }
  ],
  "materials": [
    { "cardType": "MAT1", "id": 1, "e": 210000.0, "g": 80769.0, "nu": 0.3, "rho": 7.85e-9 }
  ],
  "loads": [
    { "cardType": "FORCE", "id": 10, "subcaseId": 0, "nodeId": 4, "coordId": 0,
      "magnitude": 1000.0, "direction": [0.0, 0.0, -1.0] }
  ],
  "boundaryConditions": [
    { "cardType": "SPC1", "id": 20, "dof": "123456", "nodeIds": [1, 2, 3] }
  ],
  "caseControl": {
    "globalDirectives": {},
    "subcases": [
      { "id": 1, "directives": { "LOAD": "10", "SPC": "20" } }
    ]
  }
}
```

### 검증 결과 JSON (`<이름>_validation.json`)

```json
[
  {
    "severity": "Error",
    "cardType": "CQUAD4",
    "cardId": 1,
    "fieldName": "G",
    "message": "CQUAD4 1: 노드 ID 5에 해당하는 GRID가 존재하지 않습니다."
  }
]
```

### F06 요약 JSON (`<이름>_f06_summary.json`)

```json
{
  "fatalCount": 1,
  "warningCount": 2,
  "messages": [
    {
      "level": "Fatal",
      "lineNumber": 142,
      "message": "*** FATAL ERROR 4276: ...",
      "context": "...전후 2라인 포함..."
    }
  ]
}
```

---

## 검증 규칙

| 규칙 | 내용 |
|------|------|
| GridRule | 중복 GRID ID, 비양수 ID 검출 |
| ElementRule | 노드 ID → GRID 참조 무결성, PropertyId → Property 참조 무결성 |
| PropertyRule | MaterialId → Material 참조 무결성 |
| MaterialRule | MAT1에서 E=0 && G=0 경고 |
| LoadRule | FORCE/MOMENT NodeId → GRID 참조 무결성 |
| BcRule | SPC/SPC1/MPC NodeId → GRID 참조 무결성 |

---

## 아키텍처

```
파이프라인: BDF 파일 → CardReader → BdfParser → BdfValidator → JsonExporter → [옵션] NastranRunner → F06Parser
```

```
FemScanner/
├── Program.cs                  # 진입점, CLI 인자 처리
├── Parsers/
│   ├── BdfParser.cs            # BDF 전체 파싱 오케스트레이터
│   ├── CardReader.cs           # fixed/free/large-field 토큰 분리
│   ├── CaseControlParser.cs    # Case Control 섹션 파싱
│   └── F06Parser.cs            # F06 결과 파일 파싱
├── Models/
│   ├── BdfModel.cs             # 전체 모델 컨테이너
│   ├── Grids/Grid.cs
│   ├── Elements/               # CQUAD4, CTRIA3, CTETRA, CHEXA, CBAR, CBEAM, CROD, CONM2, RBE2
│   ├── Properties/             # PSHELL, PSOLID, PBAR, PBARL, PBEAM, PBEAML, PROD
│   ├── Materials/              # MAT1, MAT2, MAT8
│   ├── Loads/                  # FORCE, MOMENT, PLOAD, PLOAD4, GRAV
│   └── BoundaryConditions/     # SPC, SPC1, MPC
├── Validators/
│   ├── BdfValidator.cs         # 검증 오케스트레이터
│   └── Rules/                  # GridRule, ElementRule, PropertyRule, MaterialRule, LoadRule, BcRule
├── Exporters/
│   └── JsonExporter.cs         # BdfModel → JSON 직렬화
└── NastranRunner/
    └── NastranRunner.cs        # Nastran 프로세스 실행 및 F06 연동
```

---

## 제한사항

- **대형 파일**: 수십만 카드 이상의 BDF에 대한 성능 최적화 미적용
- **DMIG / DLOAD / RLOAD**: 미지원 카드는 경고와 함께 스킵
- **MAT8**: 기본 필드(E1, E2, Nu12 등)만 파싱 — 온도 의존성 필드 미지원
- **Large-field continuation**: `*` 카드의 연속 카드 병합은 기본 지원하나 복잡한 중첩 구조는 검증 미완
- **Nastran 연동**: `nastran` 명령이 시스템 PATH에 등록되어 있어야 `--nastran` 옵션 사용 가능 (Nastran 라이선스 필요)

---

## 다음 단계 후보

- **시각화**: 파싱된 메시 데이터를 3D 뷰어(WebGL 또는 VTK)로 렌더링
- **BDF 편집**: 검증 오류 자동 수정 제안 및 BDF 파일 재출력
- **MAT8 완전 지원**: 온도 의존 물성 테이블(MATT8) 파싱
- **DMIG 지원**: 강성/질량 행렬 직접 입력 카드 파싱
- **결과 비교**: 다중 F06 파일 간 FATAL/WARNING 변화 비교 리포트
- **GUI 래퍼**: WPF/MAUI 기반 데스크톱 UI 추가
