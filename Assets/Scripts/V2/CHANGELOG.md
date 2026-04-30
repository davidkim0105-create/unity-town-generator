# Town Generator V2 — Changelog

## v2.1.0 (2025-01) — 자동 도시 생성 + 시각화

### 🎯 새 기능

#### 메쉬 생성
- **도로 메쉬 (RoadMeshBuilder)**: 엣지마다 폭에 맞춘 quad 자동 생성
- **교차로 메쉬 (IntersectionMeshBuilder)**: 노드마다 N각형 원반으로 트림된 끝 마감
- **트림 시스템**: 도로 끝을 교차로 영역에서 잘라 깔끔한 마감

#### 자동 생성 도구
- **Subdivide**: 블록 클릭 → 가장 긴 변 기준 자동 분할 (LongestOpposite/TwoLongest)
- **L-System Grow**: 시드 점 클릭 → Parish & Müller 기반 도시 자동 성장
- **Road Brush**: 마우스 드래그로 일정 간격 도로 페인트
- **Width Brush**: 도로 폭을 클릭/드래그로 변경 (스포이드 지원)

#### Per-Edge Inset (자료구조 개선)
- 각 face 변마다 그 도로의 폭/2 + margin 으로 안쪽 줄임
- 폭이 다른 도로들이 섞여 있어도 빌딩 침범 없음
- `useEdgeWidthForInset` (기본 ON), `insetExtraMargin` 옵션

### 🛠 도구 정리

#### 단축키 재편
| 카테고리     | 단축키                       |
| ------------ | ---------------------------- |
| 편집 (QWERT) | Move/Draw/Cut/Delete/Connect |
| 생성 (ZXCV)  | Brush/Subdivide/L-Grow/Width |
| 해제         | Shift+Esc                    |

#### 도구 토글
- 같은 단축키/버튼 다시 누르면 도구 해제 (View 모드)
- 활성 버튼 라벨에 ✓ 표시

#### 표준 휠 컨트롤 (모든 인터랙티브 도구)
- **Ctrl + 휠** → 영역/거리 (Radius, Spacing, Max Radius 등)
- **Shift + 휠** → 크기/폭 (Width, Length 등)

### 🎨 UX 개선
- TownGenWindow에 모든 도구 통합 (인스펙터 분리 없이)
- "Build All" 버튼: 블록 + 빌딩 + 도로 메쉬 한 번에
- 도구별 옵션 자동 표시 (활성 도구에 따라)
- 권장값 시각 힌트 (인셋, 폭 등)

### 🧹 그래프 관리
- **외톨이 노드 자동 제거** 버튼 (개수 표시)
- **도로 폭 통계** 줄 (min/avg/max)
- **도로 폭 일괄 변경** 버튼 (1.5/2/3/4/6m, Custom, ×0.7/×1.4)
- L-System의 기존 도로 폭 보존 (Absorb 옵션)

### 🔧 기술
- 모든 도구에 `HandleUtility.AddDefaultControl` + `GetTypeForControl` 적용
- SafeToggle 패턴: Selection 자동 복구 + try/catch
- L-System: System.Random 기반 결정적 시드

### 📦 신규 파일

런타임 (`Assets/Scripts/V2/`):
- RoadMeshBuilder.cs
- IntersectionMeshBuilder.cs
- RoadMeshAuthoring.cs
- LSystemGenerator.cs

에디터 (`Assets/Editor/V2/`):
- RoadMeshAuthoringEditor.cs
- RoadBrushTool.cs
- SubdivideTool.cs
- LSystemGrowTool.cs
- WidthBrushTool.cs

### 🐛 알려진 한계 (v2.2에서 해결 예정)
- 곡선 도로 미지원 (직선 엣지만)
- 영역(Region) 수동 폴리곤 그리기만
- 외부 데이터 임포트 (OSM 등) 미지원
- 빌딩 프리팹 미지원 (박스만)
- 세팅 프리셋 저장/불러오기 미지원

---

## v2.2 (계획)

진행 순서:
1. ScriptableObject Preset (세팅 저장/재사용)
2. 곡선 도로 (Polyline)
3. Voronoi 자동 Region
4. 블록 변형 패턴 (ㄷ자, ㅁ자)
5. OpenStreetMap 임포트

---

## v2.0.0 (2025-01) — 초기 릴리스

### 🎯 핵심 기능
- 자유 도로 그래프 편집 (격자 제약 없음)
- Face 추출 (Half-edge 알고리즘)
- 블록 메쉬 생성 (인셋 + 두께)
- 빌딩 자동 배치 (RoadFacing / GridOBB)
- Region 시스템 (영역별 스타일)
- JSON Save/Load (자동 Build)

### 🛠 도구
- Move/Draw/Cut/Delete/Connect (Shift+Q/W/E/R/T)
- Region Paint

### 🎨 UX
- 별도 윈도우 (Alt+Shift+T)
- 단축키 + 그리드/각도 스냅
- 도구별 시각 미리보기
- 그래프 검증 + 자동 생성 프리셋