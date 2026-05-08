using UnityEditor;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    /// <summary>
    /// 윈도우 전체에서 일관된 시각 스타일.
    /// 색상, 섹션 헤더, 구분선 등.
    /// </summary>
    public static class UIStyles
    {
        // ─── 색상 ───
        public static Color SectionEditColor    = new Color(0.3f, 0.6f, 0.9f, 0.25f);   // 파랑 - 편집
        public static Color SectionBuildColor   = new Color(0.3f, 0.8f, 0.4f, 0.25f);   // 초록 - 생성
        public static Color SectionDataColor    = new Color(0.9f, 0.7f, 0.3f, 0.25f);   // 주황 - 데이터/IO
        public static Color SectionDebugColor   = new Color(0.7f, 0.7f, 0.7f, 0.25f);   // 회색 - 정보/디버그

        public static Color GroupBgColor        = new Color(0.15f, 0.15f, 0.15f, 0.4f);
        public static Color SeparatorColor      = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        // ─── 스타일 ───
        static GUIStyle _sectionHeader;
        public static GUIStyle SectionHeader
        {
            get
            {
                if (_sectionHeader == null)
                {
                    // foldout (배경 없음) 사용 → 우리가 직접 박스 그림
                    _sectionHeader = new GUIStyle(EditorStyles.foldout);
                    _sectionHeader.fontSize = 12;
                    _sectionHeader.fontStyle = FontStyle.Bold;
                    _sectionHeader.padding = new RectOffset(20, 4, 4, 4);
                    _sectionHeader.fixedHeight = 24;
                }
                return _sectionHeader;
            }
        }

        static GUIStyle _groupLabel;
        public static GUIStyle GroupLabel
        {
            get
            {
                if (_groupLabel == null)
                {
                    _groupLabel = new GUIStyle(EditorStyles.miniBoldLabel);
                    _groupLabel.fontSize = 10;
                    _groupLabel.padding = new RectOffset(4, 0, 2, 2);
                    _groupLabel.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
                }
                return _groupLabel;
            }
        }

        // ─── 헬퍼 ───

        /// <summary>색이 있는 섹션 헤더 + Foldout. 클릭 가능.</summary>
        public static bool ColoredFoldout(string key, string label, Color tint, bool defaultOpen)
        {
            const string FOLD_KEY = "TownGenV2.Fold.";
            bool current = EditorPrefs.GetBool(FOLD_KEY + key, defaultOpen);

            var rect = EditorGUILayout.GetControlRect(false, 22);

            // 1. 옅은 배경 (전체 폭)
            Color lightTint = tint;
            lightTint.a *= 0.35f;  // 더 옅게
            var bgRect = new Rect(0, rect.y, EditorGUIUtility.currentViewWidth, rect.height);
            EditorGUI.DrawRect(bgRect, lightTint);

            // 2. 짙은 헤더 박스 — 텍스트 길이에 맞춰 자동, 최소 360px
            var content = new GUIContent(label);
            float textW = SectionHeader.CalcSize(content).x;
            float headerWidth = Mathf.Max(360f, textW + 60f);
            var headerRect = new Rect(0, rect.y, headerWidth, rect.height);
            EditorGUI.DrawRect(headerRect, tint);

            // 3. 좌측 컬러 바 (강조)
            var barRect = new Rect(0, rect.y, 3, rect.height);
            Color barColor = tint; barColor.a = 1f;
            EditorGUI.DrawRect(barRect, barColor);

            // 4. Foldout (배경 없는 스타일)
            var foldRect = new Rect(rect.x + 6, rect.y + 2, rect.width - 6, 20);
            bool next = EditorGUI.Foldout(foldRect, current, label, true, SectionHeader);
            if (next != current) EditorPrefs.SetBool(FOLD_KEY + key, next);
            return next;
        }

        /// <summary>가로 구분선 (얇은 회색 선)</summary>
        public static void HorizontalLine(float thickness = 1f, float padding = 4f)
        {
            GUILayout.Space(padding);
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(thickness));
            EditorGUI.DrawRect(rect, SeparatorColor);
            GUILayout.Space(padding);
        }

        /// <summary>그룹 박스 시작 (들여쓰기 + 배경)</summary>
        public static void BeginGroup(string label = null)
        {
            EditorGUILayout.BeginVertical("box");
            if (!string.IsNullOrEmpty(label))
                GUILayout.Label(label, GroupLabel);
        }

        public static void EndGroup()
        {
            EditorGUILayout.EndVertical();
        }

        /// <summary>아이콘 라벨 (큼직한 폰트)</summary>
        static GUIStyle _bigButton;
        public static GUIStyle BigButton
        {
            get
            {
                if (_bigButton == null)
                {
                    _bigButton = new GUIStyle(GUI.skin.button);
                    _bigButton.fontSize = 12;
                    _bigButton.fontStyle = FontStyle.Bold;
                    _bigButton.fixedHeight = 32;
                }
                return _bigButton;
            }
        }

        /// <summary>"▶ Advanced" 작은 폴드아웃. 기본 접힘.</summary>
        public static bool AdvancedFoldout(string key, string label = "▶ Advanced Options")
        {
            const string ADV_KEY = "TownGenV2.Adv.";
            bool current = EditorPrefs.GetBool(ADV_KEY + key, false);

            var style = new GUIStyle(EditorStyles.miniLabel);
            style.fontStyle = FontStyle.Italic;
            style.normal.textColor = new Color(0.65f, 0.65f, 0.65f);

            var rect = EditorGUILayout.GetControlRect(false, 16);
            bool next = EditorGUI.Foldout(rect, current, label, true, style);
            if (next != current) EditorPrefs.SetBool(ADV_KEY + key, next);
            return next;
        }

        /// <summary>그룹 헤더 (작은 라벨 + 좌측 인덴트)</summary>
        public static void SubHeader(string label)
        {
            EditorGUILayout.Space(4);
            var style = new GUIStyle(EditorStyles.miniBoldLabel);
            style.fontSize = 11;
            style.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            EditorGUILayout.LabelField(label, style);
        }
    }
}