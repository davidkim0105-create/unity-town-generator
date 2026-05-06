using System.IO;
using UnityEditor;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    public static class TopDownCapture
    {
        public static int captureSize = 2048;
        public static float padding = 5f;
        public static bool transparentBackground = false;

        public static void Capture(RoadGraphAuthoring auth, string savePath)
        {
            if (auth == null || auth.graph == null || auth.graph.nodes.Count == 0)
            {
                Debug.LogWarning("[TopDownCapture] Empty graph.");
                return;
            }

            // 1) bbox 계산
            Vector3 min = auth.graph.nodes[0].position;
            Vector3 max = min;
            foreach (var n in auth.graph.nodes)
            {
                min = Vector3.Min(min, n.position);
                max = Vector3.Max(max, n.position);
            }
            min -= new Vector3(padding, 0, padding);
            max += new Vector3(padding, 0, padding);
            Vector3 center = (min + max) * 0.5f;
            float sizeX = max.x - min.x;
            float sizeZ = max.z - min.z;
            float orthoSize = Mathf.Max(sizeX, sizeZ) * 0.5f;

            // 2) 임시 카메라
            var camGO = new GameObject("__TopDownCam");
            camGO.hideFlags = HideFlags.HideAndDontSave;
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = orthoSize;
            cam.transform.position = new Vector3(center.x, max.y + 100f, center.z);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.clearFlags = transparentBackground ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
            if (transparentBackground)
                cam.backgroundColor = new Color(0, 0, 0, 0);

            // 정사각형 비율 맞추기
            float aspect = sizeX / sizeZ;
            int w = captureSize, h = captureSize;
            if (aspect > 1f) h = Mathf.RoundToInt(captureSize / aspect);
            else w = Mathf.RoundToInt(captureSize * aspect);

            // 3) 렌더
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;

            cam.Render();

            var tex = new Texture2D(w, h,
                transparentBackground ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            RenderTexture.active = prevActive;
            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(camGO);

            // 4) 저장
            byte[] png = tex.EncodeToPNG();
            File.WriteAllBytes(savePath, png);
            Object.DestroyImmediate(tex);

            Debug.Log($"[TopDownCapture] Saved: {savePath} ({w}x{h})");
            AssetDatabase.Refresh();
        }
    }
}