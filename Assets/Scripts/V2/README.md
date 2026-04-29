# 🏙️ Unity Town Generator V2

도로 그래프를 자유롭게 편집해서 도시 목업을 빠르게 만드는 Unity 에디터 도구.

watabou의 city-generator처럼 **자유로운 도로 편집** + **자동 블록/빌딩 생성**을 결합한 PCG 도구입니다.

**Version**: 2.0.0

---

## 📑 목차

- 빠른 시작
- 핵심 개념
- 주요 기능
- 단축키
- 파일 구조
- 요구사항
- 더 알아보기
- v1.x와의 관계
- 향후 계획

---

## 🚀 빠른 시작

### 윈도우 열기
- 메뉴: **TownGen V2 → Open Window**
- 단축키: **Alt+Shift+T**

### 첫 도시 만들기 (30초)
1. 윈도우의 **Create RoadGraph** 클릭
2. **Test Graphs → 5x5** 클릭 → 격자 도로 생성
3. **⚡ Build Blocks + Fill Buildings** 클릭 → 도시 완성

---

## 📋 핵심 개념

### 3단 파이프라인

**[Graph] ──build──→ [Blocks] ──fill──→ [Buildings]**
(노드+엣지) → (폴리곤 메쉬) → (박스)

| 단계               | 설명                             | 사용자 편집 |
| ------------------ | -------------------------------- | ----------- |
| **Graph**          | 도로의 노드와 엣지 (위상 정보)   | ✅ 직접 편집 |
| **Blocks**         | 그래프 face → 인셋된 폴리곤 메쉬 | ⚙️ 자동 생성 |
| **Buildings**      | 블록 안에 배치되는 빌딩          | ⚙️ 자동 생성 |
| **Regions** (선택) | 영역별 다른 빌딩 스타일          | ✅ 직접 편집 |

> 💡 **Graph가 진짜 데이터**입니다. Blocks/Buildings는 그래프에서 결정적으로 파생되므로, 같은 그래프 + 같은 세팅 + 같은 Seed면 항상 같은 도시가 나옵니다.

---

## ✨ 주요 기능

### 도로 편집
- 클릭으로 도로 그리기, 자동 교차/스냅
- 노드 이동, 다중 선택, 박스 선택
- 엣지 분할, 노드 합치기 (merge)
- 그리드 스냅 (1m), 각도 스냅 (45°)

### 자동 도시 생성
- Half-edge 알고리즘 기반 face 추출
- 정확한 polygon inset (자기교차 자동 폐기)
- 도로변 정렬 / 격자 채움 두 가지 빌딩 모드
- OBB 충돌 회피로 빌딩 겹침 방지

### Region 시스템
- 영역별 빌딩 스타일/색상 차등화
- 주거지/상업지/산업지 프리셋
- Scene 뷰에서 폴리곤 직접 그리기

### 워크플로우
- JSON Save/Load (그래프 + Region + 세팅 통합, Load 시 자동 Build)
- 자동 생성 프리셋 (Radial, Hex, Octagon, Jittered Grid)
- 그래프 무결성 검증
- 모든 항목 툴팁 + 단축키

---

## ⌨️ 단축키

### 윈도우
| 키              | 동작                |
| --------------- | ------------------- |
| **Alt+Shift+T** | TownGen 윈도우 열기 |

### 도구 활성화
| 키          | 도구    | 설명           |
| ----------- | ------- | -------------- |
| **Shift+Q** | Move    | 노드 이동      |
| **Shift+W** | Draw    | 도로 그리기    |
| **Shift+E** | Cut     | 엣지 분할      |
| **Shift+R** | Delete  | 노드/엣지 삭제 |
| **Shift+T** | Connect | 노드 합치기    |

### Draw 도구 사용 중
| 키               | 동작           |
| ---------------- | -------------- |
| **Shift** (홀드) | 45° 각도 스냅  |
| **Ctrl** (홀드)  | 1m 그리드 스냅 |
| **ESC / 우클릭** | 그리기 끊기    |

### Region Paint 도구 사용 중
| 키          | 동작             |
| ----------- | ---------------- |
| **좌클릭**  | 점 추가          |
| **우클릭**  | 마지막 점 삭제   |
| **N**       | 다음 영역 (순환) |
| **Shift+N** | 이전 영역        |
| **ESC**     | 도구 종료        |

> ⚠️ 키보드 단축키는 **Scene 뷰를 한 번 클릭한 뒤** 작동합니다. (Unity 표준)

---

## 📂 파일 구조

### Assets/Scripts/V2/ (핵심 로직, 런타임 안전)
- **RoadGraph.cs** — 그래프 자료구조 (노드+엣지)
- **RoadGraphAuthoring.cs** — Scene 컴포넌트
- **FaceExtractor.cs** — 그래프 → face 추출 (Half-edge)
- **BlockBuilder.cs** — face → 블록 메쉬
- **BuildingFiller.cs** — 블록 → 빌딩 박스
- **TownBlockV2.cs** — 블록 컴포넌트
- **PolygonTriangulator.cs** — Ear-clipping
- **PolygonUtil.cs** — 폴리곤 유틸
- **RegionData.cs** — Region 데이터
- **RegionAuthoring.cs** — Region 컨테이너 컴포넌트
- **TownDocumentV2.cs** — JSON 통합 도큐먼트
- **TownDocumentIO.cs** — JSON 입출력
- **GraphValidator.cs** — 무결성 검사
- **SnapUtil.cs** — 스냅 헬퍼
- **Version.cs** — 버전 상수
- **README.md** — 이 파일
- **TownGenV2_Manual.md** — 상세 매뉴얼
- **CHANGELOG.md** — 변경 이력

### Assets/Editor/V2/ (에디터 전용, 빌드에 안 들어감)
- **TownGenWindow.cs** — 메인 EditorWindow
- **RoadGraphAuthoringEditor.cs** — 인스펙터 커스텀
- **RegionAuthoringEditor.cs** — Region 인스펙터
- **RoadGraphGizmos.cs** — 그래프 시각화
- **RegionGizmos.cs** — Region 시각화
- **NodeMoveTool.cs** — Move 도구
- **RoadDrawTool.cs** — Draw 도구
- **RoadCutTool.cs** — Cut 도구
- **RoadDeleteTool.cs** — Delete 도구
- **NodeConnectTool.cs** — Connect 도구
- **RegionPaintTool.cs** — Region Paint 도구
- **ToolShortcuts.cs** — 단축키 정의
- **ToolUtil.cs** — 공용 헬퍼
- **AutoGraphPresets.cs** — 자동 생성 프리셋
- **TownDocMenu.cs** — Save/Load 메뉴
- **FaceExtractorSmokeTest.cs** — 알고리즘 테스트

---

## ⚙️ 요구사항

- **Unity 2022.2 이상** (FindObjectsByType, FindAnyObjectByType API 사용)
- **URP 또는 Built-in 렌더 파이프라인**
- 외부 패키지 의존성 없음

---

## 📖 더 알아보기

| 문서                    | 내용                                                     |
| ----------------------- | -------------------------------------------------------- |
| **TownGenV2_Manual.md** | 도구별 상세 사용법, 워크플로우 예시 5종, FAQ, 트러블슈팅 |
| **CHANGELOG.md**        | 버전별 변경 이력                                         |

---

## 🔄 v1.x와의 관계

v1.x (TownGenerator)는 그대로 보존됩니다.
v2.0은 **별도 컴포넌트**(RoadGraphAuthoring)로 동작하며 충돌하지 않습니다.

| 항목          | v1.x                            | v2.0                                  |
| ------------- | ------------------------------- | ------------------------------------- |
| 메인 컴포넌트 | TownGenerator                   | RoadGraphAuthoring                    |
| 도로 모델     | 격자                            | 자유 그래프                           |
| 메뉴          | Tools → Town Generator          | TownGen V2                            |
| 코드 폴더     | Assets/Scripts/, Assets/Editor/ | Assets/Scripts/V2/, Assets/Editor/V2/ |

> 같은 씬에서 v1과 v2를 동시에 사용할 수 있습니다. 데이터는 호환되지 않습니다.

---

## 🚧 향후 계획 (v2.1)

- 도로 메쉬 생성 (도로가 시각적으로 보이게)
- 교차로 메쉬
- Road Brush (드래그로 도로 그리기)
- Grow / Subdivide (블록 자동 분할)
- L-system 기반 자동 도시 성장
- 곡선 도로 (Polyline)
- 블록 변형 패턴 (ㄷ자, ㅁ자 등)
- Voronoi 기반 자동 Region
- OpenStreetMap 임포트
- ScriptableObject Preset

---

## 📝 라이선스

내부 도구. 외부 배포 시 별도 라이선스 명시 필요.