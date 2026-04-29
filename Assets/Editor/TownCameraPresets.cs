using UnityEditor;
using UnityEngine;

public static class TownCameraPresets
{
    public enum View { Top, Iso, Street, Perspective, Bird }

    public static void Apply(TownGenerator gen, View view)
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) sv = SceneView.currentDrawingSceneView;
        if (sv == null) return;

        Vector3 cityCenter = GetCityCenter(gen);
        float citySize = GetCitySize(gen);

        switch (view)
        {
            case View.Top:
                sv.in2DMode = false;
                sv.orthographic = true;
                sv.size = citySize * 0.6f;
                sv.LookAt(cityCenter, Quaternion.Euler(90, 0, 0), citySize * 0.6f, true, true);
                break;

            case View.Iso:
                sv.in2DMode = false;
                sv.orthographic = true;
                sv.size = citySize * 0.55f;
                sv.LookAt(cityCenter, Quaternion.Euler(35, 45, 0), citySize * 0.55f, true, true);
                break;

            case View.Street:
                sv.in2DMode = false;
                sv.orthographic = false;
                Vector3 streetPos = cityCenter + new Vector3(0, 4f, -citySize * 0.35f);
                sv.LookAt(cityCenter + Vector3.up * 8f,
                    Quaternion.Euler(15, 0, 0), 30f, false, true);
                break;

            case View.Perspective:
                sv.in2DMode = false;
                sv.orthographic = false;
                sv.LookAt(cityCenter, Quaternion.Euler(45, -30, 0), citySize * 0.7f, false, true);
                break;

            case View.Bird:
                sv.in2DMode = false;
                sv.orthographic = false;
                sv.LookAt(cityCenter, Quaternion.Euler(60, 0, 0), citySize * 0.8f, false, true);
                break;
        }

        sv.Repaint();
    }

    public static void FrameSelected()
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null && Selection.activeGameObject != null)
        {
            sv.FrameSelected();
        }
    }

    public static void FrameTown(TownGenerator gen)
    {
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv == null) return;
        Vector3 c = GetCityCenter(gen);
        float s = GetCitySize(gen);
        sv.LookAt(c, sv.rotation, s * 0.7f, false, true);
    }

    static Vector3 GetCityCenter(TownGenerator gen)
    {
        // TownGenerator에서 도시 중심 계산
        float aspect = Mathf.Clamp(gen.aspectRatio, 0.4f, 2.5f);
        float w = gen.townSize;
        float d = gen.townSize / aspect;
        return gen.transform.position + new Vector3(w * 0.5f, 0, d * 0.5f);
    }

    static float GetCitySize(TownGenerator gen)
    {
        float aspect = Mathf.Clamp(gen.aspectRatio, 0.4f, 2.5f);
        return Mathf.Max(gen.townSize, gen.townSize / aspect);
    }
}