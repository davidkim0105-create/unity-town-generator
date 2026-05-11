# Town Generator v2.3 — 사용 가이드

> v2.3에서 추가된 신기능 위주.
> v2.2 신기능은 `TownGenV2.2_Guide.md`, v2.1은 `TownGenV2.1_Guide.md`, v2.0 기본은 `TownGenV2_Manual.md`.

## 🎯 v2.3 핵심 변화

| 영역        | v2.2           | v2.3                                     |
| ----------- | -------------- | ---------------------------------------- |
| 윈도우 정리 | Foldout만      | **카테고리별 색 + 옵션 Simple/Advanced** |
| 도시 생성   | 도구 일일이    | **Quick Actions 한 클릭**                |
| OSM 시도    | 파일 다운 필수 | **5개 샘플 빌트인**                      |
| 빌딩 다양성 | 균일           | **블록 면적별 높이 자동 차등**           |
| Region      | 색만 다름      | **빌딩 프리셋 자동 매핑**                |
| v2 방향     | L-System 강조  | **OSM-First (실제 데이터)**              |

---

## ⌨️ 단축키 (변경 없음)

v2.2와 동일. (Shift+Backspace = 도구 해제 등)

---

## 🆕 신기능 상세

### 1. UX 재편 (v2.3.0)

#### 4 카테고리 색 구분
- 🔵 **EDIT**: 그래프 편집 도구
- 🟢 **BUILD**: 도시 생성 + 시각화
- 🟠 **DATA**: 입출력 (Save/Load, OSM, Test Graph)
- ⚪ **DEBUG**: Graph Info, Clear

각 섹션 헤더에 좌측 컬러 바 + 옅은 배경.

#### Simple / Advanced 분리
자주 쓰는 옵션만 노출, 나머지는 ▶ Advanced 안에 접힘:

| 섹션           | 노출                            | Advanced 안                 |
| -------------- | ------------------------------- | --------------------------- |
| Build Settings | Pattern, Height, Density 등 6개 | Inset, Lot 세부 9개         |
| Road Mesh      | Materials 2개                   | Y Offset, Trim 등 6개       |
| L-System       | Size, Branch, Curviness, Radius | Segment, Init, Width 등 6개 |
| Voronoi        | Mode, Random Place, Generate    | Grid, Smooth 등 4개         |

---

### 2. Quick Actions (v2.3.0) — 핵심

윈도우 → 🟢 BUILD → ⚡ Quick Actions

#### 🌐 OSM City (Main)

**Sample Cities** (다운로드 불필요):
```
Sample [🗼 Tokyo District ▼]   [Load]
```
드롭다운 5개 중 선택 → [Load] → 즉시 도시 완성.

**파일 임포트:**
- **[📂 Quick OSM City (from file)]**: 사용자 .osm → 풀 파이프라인
- **[📂 Import Only]**: 임포트만
- **[🔧 Cleanup + Build]**: 이미 임포트된 OSM 정리 + 빌드

#### 📐 Grid (Test)
- **[📐 Quick Grid City]**: 5×5 격자 + 중층 주거 (빠른 데모)

#### 🧪 L-System (Experimental, 격하)
- **[🏘 Try Village]** / **[🌆 Try Big City]**
- 결과 들쭉날쭉 (face 형성 어려움), OSM/Grid 권장

#### 🌓 Lighting
- **[☀ Day]** / **[🌙 Night]**

#### Always Regenerate 토글
- ON: 매번 그래프 새로 생성 (다른 도시)
- OFF: 그래프 있으면 빌드만 다시

---

### 3. Adaptive Building Filler (v2.3.1)

블록 면적 분포 분석 → 자동 패턴/높이.

#### 패턴 전략 (밀도 유지)
- **95%는 Solid** (빌딩 다수)
- **상위 5% 거대 블록** → Courtyard (시각 강조)
- **매우 작은 블록** → SingleTower (랜드마크)

#### 높이 자동 차등
- 면적 작은 블록 → 0.7배 높이
- 면적 큰 블록 → 1.5배 높이
- → 위에서 봐도 도시 느낌

→ Quick OSM City에 자동 적용됨.

---

### 4. Region 자동 매핑 (v2.3.1)

Voronoi 결과 Region에 빌딩 프리셋 자동 순환:

| Region 인덱스 | 적용 프리셋         | 이름/색           |
| ------------- | ------------------- | ----------------- |
| 0             | Bldg_HighCommercial | Commercial / 주황 |
| 1             | Bldg_MidResi        | MidResi / 베이지  |
| 2             | Bldg_LowResi        | LowResi / 초록    |
| 3             | Bldg_Sparse         | Sparse / 연초록   |
| 4             | Bldg_Industrial     | Industrial / 갈색 |
| 5             | Bldg_Megacity       | Megacity / 보라   |

이후 6개 넘으면 순환.

→ 같은 OSM 데이터인데 영역마다 진짜 다른 도시 분위기.

---

### 5. OSM Sample Bundle (v2.3.2)

5개 도시 빌트인:

| 샘플             | 특징                             |
| ---------------- | -------------------------------- |
| 🗼 Tokyo District | 입체교차 + 좁은 골목 (challenge) |
| 🏙 Gangnam Grid   | 한국 격자형 도심                 |
| 🗽 Manhattan Grid | 북미 직교 격자 (가장 깔끔)       |
| 🗼 Paris Radial   | 개선문 주변 방사형               |
| 🏘 Small Town     | 작은 마을 (단순)                 |

위치: `Resources/OSMSamples/*.osm.txt`

#### 추가 샘플 만들기
1. https://openstreetmap.org → Export → 영역 선택 → .osm 다운로드
2. 파일명을 `xxx.osm.txt`로 변경
3. `Assets/Resources/OSMSamples/`에 배치
4. `SampleOSMLoader.Samples` 리스트에 항목 추가

---

## 🚀 v2.3 추천 워크플로우

### 워크플로우 A: Sample 한 클릭
```
1. 윈도우 → ⚡ Quick Actions
2. Sample [🗽 Manhattan Grid ▼] → [Load]
3. → 완성 (10초)
```

### 워크플로우 B: 사용자 OSM
```
1. openstreetmap.org에서 .osm 받기
2. ⚡ Quick Actions → [📂 Quick OSM City (from file)]
3. → 완성 (15초)
```

### 워크플로우 C: 영역별 차등 도시
```
1. Sample 로드 (위)
2. → Region 자동 5개 + 빌딩 프리셋 자동 매핑됨
3. (선택) Region 인스펙터에서 Pattern/색 미세 조정
4. ⚡ Build All 다시
```

### 워크플로우 D: 도시 + 야경 + 캡처
```
1. Sample 로드
2. 🌓 Lighting → [🌙 Night]
3. 💾 Document → [📷 Capture Top-Down PNG]
4. → 야경 도시 PNG 저장
```

---

## 💡 v2.3 핵심 메시지

> **"OSM에서 도로를 가져오고, 우리 도구로 빌딩을 채운다"**

- OSM = 실제 도로 데이터 (정확)
- 우리 도구 = 빌딩 자동 채우기 (목업의 핵심 가치)
- L-System = 보조 (실험적 스케치)

---

## ⚠️ 알려진 한계 (v2.4에서 해결 가능)

| 한계              | 가능 해결                       |
| ----------------- | ------------------------------- |
| 모든 빌딩 같은 색 | v2.4: 빌딩 색 자동 다양화       |
| 도로 단순 회색    | v2.4: 도로 텍스처 + 차선        |
| Day/Night 2종만   | v2.4: Dawn/Dusk/Cinematic 추가  |
| OSM landuse 무시  | v2.4: park/commercial 인식      |
| OSM 건물 무시     | v2.4: building footprint 임포트 |
| 평지만            | v2.4: Terrain 통합              |
| 곡선 도로 X       | v2.4: Polyline 지원             |

---

## 🔗 관련 문서

- `README.md`: 프로젝트 개요
- `TownGenV2_Manual.md`: v2.0 기본
- `TownGenV2.1_Guide.md`: v2.1 신기능
- `TownGenV2.2_Guide.md`: v2.2 신기능
- `CHANGELOG.md`: 전체 이력