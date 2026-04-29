using UnityEditor;
using UnityEngine;

namespace TownGen.V2.EditorTools
{
    public static class ToolUtil
    {
        /// <summary>
        /// 마우스를 RoadGraphAuthoring의 로컬 Y=0 평면에 투영해 로컬 좌표 반환.
        /// </summary>
        public static bool TryGetMouseOnGraphPlane(
            RoadGraphAuthoring a, Event e, out Vector3 localPoint)
        {
            var tr = a.transform;
            Plane plane = new Plane(tr.up, tr.position);
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (plane.Raycast(ray, out float dist))
            {
                Vector3 world = ray.GetPoint(dist);
                localPoint = tr.InverseTransformPoint(world);
                return true;
            }
            localPoint = Vector3.zero;
            return false;
        }
    }
}