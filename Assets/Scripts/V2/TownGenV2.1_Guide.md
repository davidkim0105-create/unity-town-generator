# Town Generator v2.1 — 사용 가이드

> v2.1에서 추가된 신기능 위주. v2.0 기본 사용법은 `TownGenV2_Manual.md` 참고.

## 🎯 v2.1 핵심 변화 한눈에

| 영역        | v2.0                 | v2.1                           |
| ----------- | -------------------- | ------------------------------ |
| 도로 시각화 | 선만 보임            | 회색 메쉬 + 교차로             |
| 도로 그리기 | 클릭으로 노드 하나씩 | **드래그 페인트**              |
| 도시 생성   | 수동만               | **L-System 자동 성장**         |
| 블록 분할   | 수동 도로 추가       | **클릭 한 번 자동 분할**       |
| 도로 폭     | 모두 동일            | **변별 자동 인셋 + 일괄 변경** |
| 도구 해제   | 못 함                | **다시 누르면 토글**           |

---

## ⌨️ 단축키 전체 (v2.1)

### 편집 도구 (Shift + QWERT)
| 키      | 도구    | 동작                             |
| ------- | ------- | -------------------------------- |
| Shift+Q | Move    | 노드 이동, 박스 선택             |
| Shift+W | Draw    | 클릭으로 노드 하나씩 도로 그리기 |
| Shift+E | Cut     | 엣지 분할                        |
| Shift+R | Delete  | 노드/엣지 삭제                   |
| Shift+T | Connect | 두 노드 합치기                   |

### 생성 도구 (Shift + ZXCV) — v2.1 신규
| 키      | 도구      | 동작                           |
| ------- | --------- | ------------------------------ |
| Shift+Z | Brush     | 드래그로 일정 간격 도로 페인트 |
| Shift+X | Subdivide | 블록 클릭 → 자동 분할          |
| Shift+C | L-Grow    | L-System 도시 자동 성장        |
| Shift+V | Width     | 도로 폭 변경 (클릭/페인트)     |

### 공통
| 키          | 동작                       |
| ----------- | -------------------------- |
| Shift+Esc   | 모든 도구 해제 (View 모드) |
| Alt+Shift+T | 윈도우 열기                |
| Alt+클릭    | (Width Brush) 스포이드     |

---

## 🎡 표준 휠 컨트롤

모든 인터랙티브 도구에서 동일:
- **Ctrl + 휠**: 영역/거리 파라미터 (Radius, Spacing, Max Radius 등)
- **Shift + 휠**: 크기/폭 파라미터 (Width, Length 등)

| 도구        | Ctrl+휠         | Shift+휠       |
| ----------- | --------------- | -------------- |
| Road Brush  | Spacing         | Road Width     |
| Width Brush | Brush Radius    | Target Width   |
| Subdivide   | Min Edge Length | New Road Width |
| L-System    | Max Radius      | Segment Length |

---

## 🆕 신기능 상세

### 1. Road Mesh + Intersection Mesh

`RoadMeshAuthoring` 컴포넌트가 자동 추가됨. 윈도우 → **Road Mesh** 섹션에서:
- **Road Material / Intersection Material**: 머티리얼 슬롯 (없으면 회색 기본)
- **Trim Road Ends**: 도로 끝을 교차로 영역에서 잘라 깔끔하게
- **Intersection Coverage**: 교차로 원반의 도로 경계 겹침 정도
- **Build Road Mesh / Clear**: 단독 갱신

머티리얼을 회색만 쓸 때:
1. Project 우클릭 → Create → Material → 이름 `M_Road`
2. Base Map 옆 색상을 어두운 회색으로
3. Road Material 슬롯에 할당

### 2. Road Brush (Shift+Z)

마우스 드래그로 일정 간격마다 자동 노드+엣지 생성.

**옵션:**
- **Spacing**: 노드 간격 (1~30m)
- **Road Width**: 새 도로의 폭
- **Snap to Existing**: 기존 노드/엣지에 자동 합치기
- **Snap Distance**: 흡수 반경

**팁:**
- 드래그 중 **Shift** = 45° 각도 스냅
- 드래그 중 **Ctrl** = 1m 그리드 스냅
- 그리는 도중 **Esc** = 취소
- Spacing < Snap Distance면 자가 흡수 → 자동 보정됨

### 3. Subdivide (Shift+X)

큰 블록을 자동으로 두 개로 나눔.

**알고리즘:**
- **LongestOpposite (기본)**: 가장 긴 변 + 마주보는 평행 변의 중점을 잇는 도로 추가
- **TwoLongest**: 가장 긴 두 변

**워크플로우:**
1. Shift+X 활성
2. 블록 위에 마우스 → 주황 하이라이트 + 노란 분할 미리보기
3. 클릭 → 분할 + Auto Rebuild=ON이면 즉시 메쉬 갱신
4. 더 분할하고 싶으면 다시 클릭

**주의:**
- New Road Width를 Road Inset의 2배 이상으로 설정해야 도로가 보임
- Min Edge Length로 너무 작은 블록 보호

### 4. L-System Grow (Shift+C)

시드 점에서 도시 도로망이 자동으로 펼쳐짐.

**핵심 옵션:**
- **Iterations**: 성장 단계 (1~12, 권장 4~6)
- **Branch Probability**: 분기 확률 (0~1, 권장 0.15~0.25)
- **Angle Jitter**: 방향 무작위 (0=격자, 30°=구불구불)
- **Initial Branches**: 시작 방향 수 (4=십자, 6=별)
- **Max Radius**: 시드에서 이 거리 안쪽에서만 자람
- **Absorb Existing Edges**: ON=기존 도로 흡수, OFF=폭 보존 (권장)

**프리셋:**

| 스타일      | Iter | Branch P | Angle | Init |
| ----------- | ---- | -------- | ----- | ---- |
| 작은 마을   | 3    | 0.15     | 25°   | 4    |
| 격자 신도시 | 5    | 0.3      | 5°    | 4    |
| 자연 발생   | 5    | 0.18     | 25°   | 4    |
| 방사형      | 4    | 0.2      | 15°   | 6    |
| 거대 도시   | 6    | 0.25     | 20°   | 4    |

**주의:**
- Branch P > 0.4 = 폭주
- Iterations > 8 + 높은 Branch P = 노드 수천 개

### 5. Width Brush (Shift+V)

도로 폭을 시각적으로 변경.

**모드:**
- **Click**: 클릭한 엣지만 변경
- **Paint**: 드래그로 반경 내 모든 엣지에 적용

**팁:**
- **Ctrl+휠** = Brush Radius 즉시 조절
- **Shift+휠** = Target Width 즉시 조절
- **Alt+클릭** = 스포이드 (그 엣지 폭을 Target에 복사)

**워크플로우 예시:**
1. L-System으로 마을 생성 (모두 2.5m)
2. Width Brush, Target=5
3. 마을 중심 가로지르는 도로 클릭으로 5m 승격
4. → 위계 있는 도시 (간선 + 골목)

### 6. Per-Edge Inset

face의 각 변이 그 도로의 폭에 맞춰 자동으로 다른 거리로 인셋.

**옵션 (Build Settings):**
- **Use Edge Width Inset** (기본 ON): 변별 자동 인셋
- **Inset Extra Margin**: 도로 폭/2에 더하는 여백 (=보도 넓이)

**Margin 가이드:**
- 0 = 도로변 상가 (보도 없음)
- 0.5 = 일반 신도시 (약간 보도)
- 1.5 = 강남 느낌 (넓은 보도)
- 3 = 공원 도로

OFF로 두면 v2.0 동작 (단일 Road Inset).

### 7. 도로 폭 일괄 변경 (Graph Info 섹션)

- **통계**: min / avg / max 표시
- **프리셋 버튼**: 1.5m / 2m / 3m / 4m / 6m
- **Custom**: 직접 입력 + Apply
- **× 0.7 / × 1.4**: 비율로 일괄 조정

### 8. 외톨이 노드 자동 제거

Graph Info 섹션에 개수 표시 + 한 번에 정리.
예시 라벨: `🗑 Remove Orphan Nodes (12)` — 12개 있을 때만 활성화.

---

## 🚀 v2.1 추천 워크플로우

### 워크플로우 A: 큰 도시 빠르게
1. L-Grow (Shift+C) 시드 클릭 → 자동 도시
2. Subdivide (Shift+X) 큰 블록 한두 번 클릭
3. Width Brush (Shift+V) 중심 도로 굵게
4. [Build All]

### 워크플로우 B: 디자인 도시
1. Brush (Shift+Z)로 메인 도로 굵게 그리기 (4m)
2. L-Grow (Shift+C) Absorb=OFF로 골목 추가
3. 통계 확인: min 2.5  avg 3.0  max 4.0
4. [Build All]

### 워크플로우 C: 격자 도시
1. Test Graphs → 5x5
2. Subdivide로 일부 큰 블록 더 분할
3. Width Brush로 외곽은 좁게, 중심은 굵게
4. [Build All]

---

## 📊 v2.1 도구 정리표

| 도구      | 단축키  | 입력         | 결과           |
| --------- | ------- | ------------ | -------------- |
| Move      | Shift+Q | 드래그       | 노드 이동      |
| Draw      | Shift+W | 클릭×N       | 노드 연속 추가 |
| Cut       | Shift+E | 클릭         | 엣지 분할      |
| Delete    | Shift+R | 클릭         | 노드/엣지 삭제 |
| Connect   | Shift+T | 두 노드 클릭 | 합치기         |
| Brush     | Shift+Z | 드래그       | 일정 간격 도로 |
| Subdivide | Shift+X | 클릭         | 블록 분할      |
| L-Grow    | Shift+C | 클릭         | 도시 자동 성장 |
| Width     | Shift+V | 클릭/드래그  | 도로 폭 변경   |

---

## ⚠️ 알려진 한계 (v2.2에서 해결 예정)

- 곡선 도로 없음 (직선만)
- Region 수동 그리기만 (자동화 없음)
- OSM 등 외부 데이터 임포트 없음
- 빌딩 박스만 (프리팹 미지원)
- 세팅 프리셋 저장 없음 → v2.2 첫 작업으로 추가 예정

---

## 🔗 관련 문서

- `README.md`: 프로젝트 개요
- `TownGenV2_Manual.md`: v2.0 기본 사용법
- `CHANGELOG.md`: 전체 변경 이력