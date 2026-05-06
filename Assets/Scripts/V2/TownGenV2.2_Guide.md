# Town Generator v2.2 — 사용 가이드

> v2.2에서 추가된 신기능 위주.
> v2.1 신기능은 `TownGenV2.1_Guide.md`, v2.0 기본은 `TownGenV2_Manual.md` 참고.

## 🎯 v2.2 핵심 변화

| 영역          | v2.1               | v2.2                                  |
| ------------- | ------------------ | ------------------------------------- |
| 세팅 관리     | 슬라이더 직접 조절 | **ScriptableObject Preset**           |
| 도시 결과물   | 화면에서만 확인    | **Top-Down PNG 저장**                 |
| Region 폴리곤 | 손으로 그리기      | **Voronoi 자동 분할**                 |
| 블록 정보     | Hierarchy 클릭으로 | **호버로 즉시 표시**                  |
| 빌딩 모양     | 박스 격자/도로변만 | **6가지 패턴 (ㅁ/ㄷ/ㄴ/안마당/타워)** |
| 영역별 차등   | 색/높이만          | **+ 패턴 차등**                       |
| 외부 데이터   | 직접 그리기만      | **OSM 임포트**                        |
| 라이팅        | Unity 기본         | **Day/Night 프리셋**                  |
| 윈도우        | 길게 펼쳐짐        | **섹션 Foldout**                      |

---

## ⌨️ 단축키 (v2.2 추가)

| 키              | 도구       | 동작                     |
| --------------- | ---------- | ------------------------ |
| Shift+F         | Voronoi    | 시드 점 → 자동 영역 분할 |
| Shift+A         | Inspect    | 블록 호버 정보           |
| Shift+Backspace | Deactivate | 모든 도구 해제           |

(v2.1까지의 단축키는 그대로)

---

## 🆕 신기능 상세

### 1. Preset 시스템

세팅을 에셋으로 저장 → 매번 슬라이더 만지지 않아도 됨.

#### 만들기
1. Project 우클릭 → Create → TownGen V2:
   - **L-System Preset**
   - **Building Preset**
   - **Town Preset** (Build Settings용)
2. 인스펙터에서 값 편집

#### 사용
1. 윈도우의 각 섹션 위쪽에 **Preset 슬롯**:
   ```
   Town Preset  [None ▼]  [Apply]  [Save As...]
   ```
2. 슬롯에 에셋 끌어다 놓기
3. **[Apply]** 클릭 → 즉시 적용
4. **[Save As...]** → 현재 세팅을 새 에셋으로 저장

#### 빌트인 프리셋 (17개)
메뉴 → **TownGen V2 → Create Built-in Presets** 클릭하면:

```
Resources/Presets/
├─ LSystem/  (SmallVillage, GridCity, Natural, Radial, BigCity, LinearTown)
├─ Building/ (LowResi, MidResi, HighCommercial, Industrial, Sparse, Megacity)
└─ Town/     (Default, NoSidewalk, WideSidewalk, ParkRoad, LegacyV20)
```

---

### 2. Top-Down Capture

도시 미니맵 PNG 저장.

**위치:** 윈도우 → 💾 Document / Capture 섹션

**옵션:**
- **Size**: 1K / 2K / 4K / 8K
- **Transparent BG**: 배경 투명 PNG (스카이박스 안 찍힘)

**[📷 Capture Top-Down PNG]** → 파일 저장 다이얼로그.

---

### 3. Voronoi 자동 Region (Shift+F)

시드 점 N개 클릭 → 자동 영역 분할.

#### 워크플로우
1. **Shift+F** Voronoi 도구 활성
2. Scene에 시드 점 클릭 (또는 윈도우 [Random Place])
3. **Mode** 선택:
   - **OnePerSeed** (추천): 시드마다 새 Region 자동 생성
   - **CycleRegions**: 기존 Region에 순차 매핑
   - **RandomRegions**: 무작위 매핑
   - **AllSame**: 모두 첫 Region에
4. **[✨ Generate Regions]** 클릭

#### 옵션
- **Grid Resolution** (Ctrl+휠): 0.3~5m. 작을수록 정확, 느림
- **Smoothing Passes**: 경계 평활화 (0=거침, 3=부드러움)
- **Simplify Tolerance**: Douglas-Peucker 단순화 (m)
- **Bounds Padding**: 외곽 여유

#### 입력
| 입력    | 동작                      |
| ------- | ------------------------- |
| 좌클릭  | 시드 추가                 |
| 우클릭  | 가까운 시드 제거          |
| Ctrl+휠 | Grid Resolution 즉시 조절 |

---

### 4. Block Inspector (Shift+A)

블록 호버로 정보 확인.

표시 정보:
- 블록 이름
- 꼭짓점 수, 면적
- 빌딩 수
- 매칭된 Region

**클릭 → Hierarchy에서 그 블록 선택**.

---

### 5. 블록 변형 패턴 6종

윈도우 → ⚙ Build Settings → **Block Pattern**:

| Pattern         | 모양                     | 용도             |
| --------------- | ------------------------ | ---------------- |
| **Solid**       | 칸칸이 박스 (기존)       | 일반             |
| **Perimeter**   | ㅁ자 외곽 띠 + 안마당 빔 | 아파트 단지      |
| **UShape**      | ㄷ자 (한 변 비움)        | 광장 면한 단지   |
| **LShape**      | ㄴ자 (두 변 비움)        | 모서리           |
| **Courtyard**   | 두꺼운 띠 + 통일 높이    | 모던 단지        |
| **SingleTower** | 가운데 큰 박스 1개       | 랜드마크/큰 블록 |

**Perimeter Depth**: 띠 두께 (Solid/SingleTower일 땐 안 보임).
- 블록보다 크면 자동 제한 (35% 한도)
- 너무 작은 블록은 SingleTower로 fallback

#### Region별 패턴
TownRegionV2 인스펙터에 Pattern 필드 추가됨:
- 주거지 = Perimeter
- 상업지 = SingleTower
- 외곽 = Solid
같은 차등 가능.

Region 영역 안 = Region 패턴, 밖 = 윈도우의 글로벌 패턴.

---

### 6. 통계 (Graph Info)

윈도우 → ℹ Graph Info:
```
Blocks: 16   Buildings: 245   Total Area: 1450 m²
By Region: Cell_0=3  Cell_1=2  ...
```

---

### 7. OSM 임포트 🌐

실제 도시 데이터 가져오기.

#### .osm 파일 받기
1. https://www.openstreetmap.org 접속
2. 영역 선택 (지도 우상단 [내보내기/Export])
3. **수동으로 다른 영역 선택**으로 박스 그리기 (200~500m 권장)
4. **[내보내기]** → `.osm` 파일 다운로드

또는 https://overpass-turbo.eu 에서 쿼리 후 다운로드.

⚠️ 큰 영역(km 단위)은 노드 수만 개 → Unity 매우 느려짐.

#### Unity로 임포트
1. 윈도우 → 🌐 OSM Import 섹션
2. **옵션**:
   - **Clear Graph First**: 임포트 전 기존 그래프 삭제
   - **Use OSM Road Widths**: ON=highway 종류별 자동, OFF=일괄 폭
   - **Default Width**: OFF일 때의 폭
   - **Scale Factor**: 1.0=실제, 0.5=절반, 0.1=1/10 (미니어처)
   - **Exclude Footways**: footway/path/cycleway 제외 (단순화)
3. **[📂 Import .osm File...]** → 파일 선택
4. 다이얼로그에 결과 표시
5. **[⚡ Build All]** → 블록/빌딩 자동 생성

#### highway → 폭 매핑
| highway                    | 폭 (m) |
| -------------------------- | ------ |
| motorway                   | 10     |
| trunk                      | 8      |
| primary                    | 6      |
| secondary                  | 5      |
| tertiary                   | 4      |
| residential / unclassified | 3.5    |
| service                    | 2.5    |
| living_street              | 3      |
| pedestrian                 | 2      |
| footway / path / cycleway  | 1.5    |

#### OSM 임포트 후 권장 워크플로우
```
1. Graph Info → [Validate Graph] (무결성 확인)
2. [🗑 Remove Orphan Nodes] (외톨이 정리)
3. (필요시) Set All Widths → [× 0.7] 두 번 (도로 폭 축소)
4. Build Settings:
   - Use Edge Width Inset: OFF
   - Road Inset: 1.5
   - Block Pattern: SingleTower 또는 Solid
5. [⚡ Build All]
6. 큰 빈 블록 → Shift+X로 분할
```

---

### 8. Night Lighting (보너스)

윈도우 → 🌙 Lighting Preset:

**[☀ Day Preset]**: 주간 (밝은 흰빛)
**[🌙 Night Preset]**: 야경 (어두운 파랑) + 빌딩 emission

**Night Emission Color**: 빌딩 창문 빛 색상 (따뜻한 노랑/주황 추천)

⚠️ Night 적용 후 Build All 다시 하면 emission 풀림. **Build → Night** 순서.

---

### 9. 윈도우 Foldout

모든 섹션 접기/펼치기:
- 섹션 헤더 클릭 → 토글
- 상단 **[Expand All]** / **[Collapse All]** / **[Default]**
- 상태는 EditorPrefs에 저장 (다음에 윈도우 열면 그대로)

기본 펼침 상태:
- 🛠 Tools (펼침)
- ⚙ Build Settings (펼침)
- ▶ Build (펼침)
- 나머지는 접힘

---

## 🚀 v2.2 추천 워크플로우

### 워크플로우 A: OSM 실제 도시
```
1. .osm 파일 다운 (작은 영역, 200~500m)
2. 🌐 OSM Import → Import → Build All
3. (필요시) 도로 폭 [× 0.7]
4. Block Pattern = SingleTower
5. 🌙 Night Preset
6. 📷 Top-Down Capture
```

### 워크플로우 B: 자동 도시 + 영역 차등
```
1. LS Preset = LS_BigCity → Apply
2. Shift+C로 시드 클릭 → 자동 도시
3. Create Regions → Add Residential, Add Commercial
4. Residential.pattern = Perimeter (depth 8)
   Commercial.pattern = SingleTower
5. Shift+F (Voronoi) → Random Place 4 → CycleRegions → Generate
6. ⚡ Build All
7. 🌙 Night Preset
```

### 워크플로우 C: 빠른 마을
```
1. LS Preset = LS_SmallVillage → Apply
2. Shift+C → 클릭
3. Bldg Preset = Bldg_LowResi → Apply
4. ⚡ Build All
```

---

## ⚠️ 알려진 한계 (v2.3에서 해결 가능)

- **곡선 도로 미지원** (직선만, OSM 곡선도 짧은 직선 다수로)
- **OSM 입체교차 평면 처리** (face 형성 일부 방해)
- **Region당 폴리곤 1개** (다중 영역 미지원)
- **빌딩 박스만** (목업이므로 의도적)
- **Region 자동 무결성 검증 X** (Voronoi 결과를 손으로 편집 시)

---

## 🔗 관련 문서

- `README.md`: 프로젝트 개요
- `TownGenV2_Manual.md`: v2.0 기본 사용법
- `TownGenV2.1_Guide.md`: v2.1 신기능 가이드
- `CHANGELOG.md`: 전체 변경 이력