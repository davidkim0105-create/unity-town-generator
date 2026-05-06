using UnityEngine;

namespace TownGen.V2
{
    /// <summary>
    /// 야경/주간 라이팅 프리셋.
    /// 직접 라이트와 ambient만 변경. 빌딩 emission은 머티리얼 옵션이라 별도.
    /// </summary>
    public static class NightPreset
    {
        public enum Mode { Day, Night }

        public static void Apply(Mode m)
        {
            // Directional Light 찾기 (씬에 첫 번째 것)
            Light dir = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) { dir = l; break; }
            }

            if (m == Mode.Night)
            {
                if (dir != null)
                {
                    dir.color = new Color(0.4f, 0.5f, 0.7f);
                    dir.intensity = 0.25f;
                    dir.transform.rotation = Quaternion.Euler(20f, -30f, 0f);
                }
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.15f, 0.18f, 0.28f);
                RenderSettings.fogColor = new Color(0.05f, 0.08f, 0.15f);
            }
            else
            {
                if (dir != null)
                {
                    dir.color = Color.white;
                    dir.intensity = 1f;
                    dir.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                }
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);
                RenderSettings.fogColor = new Color(0.5f, 0.5f, 0.5f);
            }
        }

        /// <summary>
        /// 모든 빌딩에 emission 적용 (야경 창문 효과).
        /// </summary>
        public static void ApplyBuildingEmission(RoadGraphAuthoring auth, bool enable, Color emissionColor)
        {
            if (auth == null) return;
            var blocksRoot = auth.transform.Find("_Blocks");
            if (blocksRoot == null) return;

            int count = 0;
            foreach (Transform block in blocksRoot)
            {
                foreach (Transform building in block)
                {
                    var mr = building.GetComponent<MeshRenderer>();
                    if (mr == null) continue;
                    var mat = mr.sharedMaterial;
                    if (mat == null) continue;

                    if (enable)
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", emissionColor * Random.Range(0.5f, 2f));
                    }
                    else
                    {
                        mat.DisableKeyword("_EMISSION");
                    }
                    count++;
                }
            }
            Debug.Log($"[NightPreset] Building emission {(enable ? "enabled" : "disabled")} on {count} buildings.");
        }
    }
}