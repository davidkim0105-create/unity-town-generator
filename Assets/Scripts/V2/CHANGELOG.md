# Town Generator V2 — Changelog

## v2.3.2 (2025-01) — OSM Sample Bundle

### 🌐 OSM Samples (5개 빌트인)
- Tokyo District (입체교차 + 골목)
- Gangnam Grid (한국 격자)
- Manhattan Grid (북미 직교)
- Paris Radial (방사형)
- Small Town (작은 마을)

### Sample Loader
- `SampleOSMLoader`: Resources/OSMSamples/ 자동 로드
- 윈도우에 Sample 드롭다운 + Load 버튼
- 다운로드 없이 한 클릭으로 도시 시도

---

## v2.3.1 — Adaptive Building Filler

### 🏢 Adaptive 빌딩
- `AdaptiveBuildingFiller`: 블록 면적 분포(percentile) 분석
- 패턴: 95% Solid (밀도 유지), 거대 블록만 Courtyard (시각 강조)
- 높이 자동 차등 (작은 블록 ↓, 큰 블록 ↑)

### 🎨 Region 자동 매핑
- `RegionAutoAssigner`: Voronoi 결과에 6개 빌딩 프리셋 순환 적용
- Commercial / MidResi / LowResi / Sparse / Industrial / Megacity
- 자동 색/이름 부여

---

## v2.3.0 — UX 정리 + OSM-First Quick Actions

### 🎨 UX 재편
- `UIStyles`: 공통 시각 시스템 (색 Foldout, 헤더, 버튼)
- 4 카테고리 (EDIT/BUILD/DATA/DEBUG) 색 구분
- 전 섹션 Foldout + EditorPrefs 영속

### 🛠 옵션 정리 (Simple/Advanced)
- Build Settings: 16 → 6 + Advanced
- Road Mesh: 8 → 2 + Advanced
- L-System: 11 → 4 + Advanced
- Voronoi: 9 → 4 + Advanced
- Subdivide: 5 → 1 + Advanced

### ⚡ Quick Actions (한 클릭 워크플로우)
- 🌐 Quick OSM City (★ 메인): 파일 → 임포트 → 정리 → Voronoi → Build
- 🔧 Cleanup + Build
- 📐 Quick Grid City (테스트)
- 🧪 L-System (Experimental, 격하)
- ☀ Day / 🌙 Night

### 🎯 v2 방향 명문화
- **OSM = 도로 데이터 (실제)** + **우리 도구 = 빌딩 채우기 (가치)**
- L-System은 보조 (실험적 스케치)

### 자동화
- 외톨이 자동 제거
- 도로 폭 자동 0.5배 (motorway 10m → 5m)
- GridOBB + fillInteriorRows
- SubdivideLargeBlocks (4 iter, ratio-aware)

---

## v2.2.3 — OSM Import + Night Lighting

### 🌐 OSM Import
- `OSMParser` (XML), `OSMImporter` (way → RoadGraph)
- highway 종류별 자동 폭

### 🌙 Night Lighting (보너스)
- Day/Night 토글, 빌딩 emission

---

## v2.2.2 — Block Patterns + Window Foldouts

### 🏗 블록 패턴 6종
- Solid / Perimeter / UShape / LShape / Courtyard / SingleTower
- Region별 패턴

### 🎨 윈도우 정리
- Foldout화 + 일괄 펼침/접기

---

## v2.2.1 — Voronoi Region + Block Stats

### ◇ Voronoi 자동 Region
- 격자 샘플링 + 가장 가까운 시드 + 경계 추출
- 4 모드 (OnePerSeed / CycleRegions / RandomRegions / AllSame)

### 🔍 Block Inspector + 통계
- 호버 정보, 블록 수/빌딩 수/Region별 분포

---

## v2.2.0 — Presets + Top-Down Capture

### 🎁 Preset 시스템
- LSystem / Building / Town Preset (ScriptableObject)
- 17개 빌트인

### 📷 Top-Down Capture
- PNG 출력 (1K~8K, 투명 배경 옵션)

---

## v2.1.0 — 자동 도시 생성 + 시각화

- 도로 메쉬 + 교차로 메쉬
- Subdivide / L-System Grow / Road Brush / Width Brush
- Per-Edge Inset
- 도구 토글, 표준 휠 컨트롤
- 외톨이 노드 제거, 도로 폭 일괄

---

## v2.0.0 — 초기 릴리스

- 자유 도로 그래프 편집
- Face 추출, 블록 메쉬, 빌딩 자동 배치
- Region 시스템, JSON Save/Load
- 별도 윈도우, 단축키, 자동 생성 프리셋