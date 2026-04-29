using System.IO;
using UnityEngine;

namespace TownGen.V2
{
    public static class TownDocumentIO
    {
        public static void Save(TownDocumentV2 doc, string path)
        {
            string json = JsonUtility.ToJson(doc, prettyPrint: true);
            File.WriteAllText(path, json);
            Debug.Log($"[TownDoc] Saved: {path} ({json.Length} chars)");
        }

        public static TownDocumentV2 Load(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[TownDoc] File not found: {path}");
                return null;
            }
            string json = File.ReadAllText(path);
            var doc = JsonUtility.FromJson<TownDocumentV2>(json);
            doc?.graph?.InvalidateCache();
            Debug.Log($"[TownDoc] Loaded: {path} (nodes={doc?.graph?.nodes?.Count}, edges={doc?.graph?.edges?.Count})");
            return doc;
        }
    }
}