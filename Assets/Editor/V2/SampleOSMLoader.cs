using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    /// <summary>
    /// Resources/OSMSamples/ 안의 .osm.txt 샘플 파일들을 로드.
    /// </summary>
    public static class SampleOSMLoader
    {
        public class Sample
        {
            public string displayName;   // UI 표시명
            public string resourcePath;  // Resources 경로 (확장자 없음)
            public string description;
        }

        public static readonly List<Sample> Samples = new List<Sample>
        {
            new Sample
            {
                displayName  = "🗼 Tokyo District",
                resourcePath = "OSMSamples/tokyo_district.osm",
                description  = "도쿄 도심 — 입체교차 + 좁은 골목 많음"
            },
            new Sample
            {
                displayName  = "🏙 Gangnam Grid",
                resourcePath = "OSMSamples/gangnam_grid.osm",
                description  = "강남역 — 한국 격자형 도심"
            },
            new Sample
            {
                displayName  = "🗽 Manhattan Grid",
                resourcePath = "OSMSamples/manhattan_grid.osm",
                description  = "맨해튼 — 북미 직교 격자 (가장 깔끔)"
            },
            new Sample
            {
                displayName  = "🗼 Paris Radial",
                resourcePath = "OSMSamples/paris_radial.osm",
                description  = "개선문 주변 — 방사형 도시"
            },
            new Sample
            {
                displayName  = "🏘 Small Town",
                resourcePath = "OSMSamples/small_town.osm",
                description  = "작은 마을 (단순 도로망)"
            }
        };

        /// <summary>
        /// 샘플 .osm 텍스트를 Resources에서 로드.
        /// 실패 시 null.
        /// </summary>
        public static string LoadOSMText(Sample sample)
        {
            if (sample == null) return null;
            var asset = Resources.Load<TextAsset>(sample.resourcePath);
            if (asset == null)
            {
                Debug.LogWarning($"[SampleOSMLoader] Sample not found: " +
                                 $"Resources/{sample.resourcePath}.txt\n" +
                                 "확장자가 .osm.txt 인지 확인하세요.");
                return null;
            }
            return asset.text;
        }

        /// <summary>
        /// 샘플을 임시 파일로 저장하고 그 경로 반환.
        /// (OSMImporter가 파일 경로 기반이라서)
        /// </summary>
        public static string SaveToTempFile(Sample sample)
        {
            string text = LoadOSMText(sample);
            if (string.IsNullOrEmpty(text)) return null;

            string tempDir = Path.Combine(Application.temporaryCachePath, "TownGenOSMSamples");
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

            string fileName = Path.GetFileName(sample.resourcePath);  // "tokyo_district.osm"
            string tempPath = Path.Combine(tempDir, fileName);

            File.WriteAllText(tempPath, text);
            return tempPath;
        }
    }
}