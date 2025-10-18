using UnityEngine;
using UnityEditor;
using System.Linq;

public class DeleteUnusedAssets : EditorWindow
{
    [MenuItem("Tools/Delete Unused Materials & Textures")]
    static void ShowWindow()
    {
        GetWindow(typeof(DeleteUnusedAssets), false, "Delete Unused Assets");
    }

    void OnGUI()
    {
        if (GUILayout.Button("List & Delete Unused"))
        {
            DeleteUnused("Assets/code.JEON 2/pre_AlchemistShop/Textures");
        }
    }

    static void DeleteUnused(string folderPath)
    {
        // 폴더 내 모든 텍스처와 머티리얼
        string[] allAssets = AssetDatabase.FindAssets("t:Texture2D t:Material", new[] { folderPath })
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .ToArray();

        // 씬에서 실제 사용되는 모든 의존성
        string[] usedAssets = AssetDatabase.GetDependencies("Assets", true);

        int deletedCount = 0;
        foreach (var asset in allAssets)
        {
            if (!usedAssets.Contains(asset))
            {
                Debug.Log("Deleting unused asset: " + asset);
                AssetDatabase.DeleteAsset(asset);
                deletedCount++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Deleted {deletedCount} unused assets.");
    }
}
