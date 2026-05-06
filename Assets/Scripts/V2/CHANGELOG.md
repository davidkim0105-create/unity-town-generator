# Town Generator V2 — Changelog

## v2.2.3 (2025-01) — OSM Import + Night Lighting

### 🌐 OSM Import
- **OSMParser**: XML 파싱, 위경도 → Unity 미터 (등거리 평면 투영)
- **OSMImporter**: way[highway=*] → RoadGraph 변환
- highway 종류별 자동 도로 폭 (motorway 10m ~ footway 1.5m)
- 옵션: clearGraphFirst, useOSMRoadWidths, scaleFactor, excludeFootways

### 🌙 Night Lighting Preset (보너스)
- Day/Night 토글 (Directional Light + Ambient + Fog 변경)
- 빌딩 emission on/off (창문 효과)
- Color picker로 emission 색상

---

## v2.2.2 — Block Patterns + Window Foldouts

### 🏗 블록 변형 패턴 6종
- Solid, Perimeter (ㅁ자), UShape (ㄷ자), LShape (ㄴ자), Courtyard, SingleTower
- Region별 다른 패턴 적용 가능 (TownRegionV2.pattern)
- Perimeter 두께 자동 제한 (블록보다 크면 fallback)

### 🎨 윈도우 정리
- 모든 섹션 Foldout화 (EditorPrefs 영속)
- [Expand All] / [Collapse All] / [Default] 버튼
- 섹션 아이콘 + 권장 펼침 상태

---

## v2.2.1 — Voronoi Region + Block Stats

### ◇ Voronoi 자동 Region
- VoronoiRegionGenerator: 격자 샘플링 + 가장 가까운 시드 + 경계 추출
- VoronoiTool (Shift+F): 시드 점 클릭, 우클릭 제거, Ctrl+휠 그리드 해상도
- 4가지 모드: OnePerSeed (자동 생성), CycleRegions, RandomRegions, AllSame
- Random Place + Generate 버튼
- 경계 평활화 + Douglas-Peucker 단순화

### 🔍 Block Inspector (Shift+A)
- 블록 호버로 면적/꼭짓점/빌딩 수/Region 표시
- 클릭으로 그 블록 GameObject 선택

### 📊 통계
- Graph Info: 블록 수, 빌딩 수, 총 면적
- Region별 분포 표시

---

## v2.2.0 — ScriptableObject Presets + Top-Down Capture

### 🎁 Preset 시스템
- LSystemPresetV2 / BuildingPresetV2 / TownPresetV2 (ScriptableObject)
- PresetGUIHelper: 공통 "Slot + Apply + Save As" 한 줄 UI
- TownGenWindow 통합 (Build Settings, Buildings, L-System)
- BuiltInPresetCreator 메뉴: 17개 빌트인 프리셋 일괄 생성
  - LSystem: SmallVillage, GridCity, Natural, Radial, BigCity, LinearTown
  - Building: LowResi, MidResi, HighCommercial, Industrial, Sparse, Megacity
  - Town: Default, NoSidewalk, WideSidewalk, ParkRoad, LegacyV20

### 📷 Top-Down Capture (보너스)
- 도시를 위에서 본 PNG 출력 (정사영 카메라)
- 사이즈: 1K/2K/4K/8K, 투명 배경 옵션
- 자동 bbox + 카메라 배치
- TownGenWindow → Document 섹션

---

## v2.1.0 (2025-01) — 자동 도시 생성 + 시각화

### 🎯 핵심
- 도로 메쉬 + 교차로 메쉬 (RoadMeshBuilder, IntersectionMeshBuilder)
- Subdivide 자동 분할 (Shift+X)
- L-System 자동 도시 성장 (Shift+C)
- Road Brush 드래그 페인트 (Shift+Z)
- Width Brush 도로 폭 변경 (Shift+V)
- Per-Edge Inset (변별 자동 인셋)

### 🛠 UX
- 단축키 재편: QWERT(편집) + ZXCV(생성)
- 도구 토글 (다시 누르면 해제)
- 표준 휠 컨트롤 (Ctrl=영역, Shift=크기)
- 외톨이 노드 자동 제거
- 도로 폭 일괄 변경 + 통계
- L-System 폭 보존 옵션

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