# Town Generator V2 — Changelog

## v2.0.0 (2025-01) — 초기 릴리스

### 🎯 핵심 기능
- **자유 도로 그래프 편집**: 격자 제약 없이 노드/엣지를 자유 배치
- **자동 face 추출**: 그래프에서 폐곡선(face) 자동 인식
- **블록 메쉬 생성**: face → 인셋된 폴리곤 메쉬 (보도블록 두께 옵션)
- **빌딩 자동 배치**: 도로변 정렬 또는 격자 채움 두 모드
- **Region 시스템**: 영역별 빌딩 스타일/색상 차등
- **JSON 저장/로드**: 그래프 + 영역 + 세팅 통합 저장, 로드 시 자동 빌드

### 🛠 도구
- Move (Shift+Q): 노드 이동, 다중 선택, 박스 선택
- Draw (Shift+W): 도로 그리기, 자동 교차/스냅
- Cut (Shift+E): 엣지 분할
- Delete (Shift+R): 노드/엣지 삭제
- Connect (Shift+T): 노드 합치기
- Region Paint: 영역 폴리곤 그리기 (N=다음, Shift+N=이전)

### 🎨 UX
- 별도 윈도우 (Alt+Shift+T)
- 단축키 + 그리드/각도 스냅 (Shift/Ctrl)
- 도구별 시각 미리보기
- 그래프 검증 (자기교차/고립 노드 등)
- 자동 생성 프리셋 (Radial, Hex, Octagon, Jittered)
- 모든 항목 툴팁

### 📂 기술 스택
- Pure C# 그래프 자료구조 (RoadGraph, RoadNode, RoadEdge)
- Half-edge 기반 face 추출 알고리즘
- Ear-clipping 폴리곤 삼각화
- Edge offset 기반 폴리곤 인셋
- OBB 기반 빌딩 충돌 회피

### 🔄 v1.x와의 관계
- 별도 컴포넌트 (RoadGraphAuthoring), 충돌 없이 공존
- v1 → v2 자동 변환은 미지원

---

## v2.1 (계획)

다음 트랙에서 추가 예정:
1. 도로 메쉬 생성 (지금은 빈 공간)
2. 교차로 메쉬
3. Road Brush 도구 (드래그로 그리기)
4. Grow / Subdivide (블록 자동 분할)
5. L-system 자동 도시 성장
6. 곡선 도로 (Polyline / Bezier)
7. 블록 변형 패턴 (ㄷ자, ㅁ자 등)
8. Voronoi 기반 자동 Region
9. OpenStreetMap 임포트
10. ScriptableObject Preset