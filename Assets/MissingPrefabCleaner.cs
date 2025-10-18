using UnityEditor;
using UnityEngine;

public class MissingPrefabCleaner
{
    [MenuItem("Tools/Clean Missing Prefabs In Scene")]
    static void CleanMissingPrefabs()
    {
        int count = 0;
        var allObjects = GameObject.FindObjectsOfType<GameObject>();

        foreach (var go in allObjects)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(go))
            {
                var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(go);
                if (prefabAsset == null)
                {
                    GameObject.DestroyImmediate(go);
                    count++;
                }
            }
        }

        Debug.Log($"🧹 Removed {count} missing prefab instance(s) from the scene.");
    }
}
