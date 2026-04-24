#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static class EditorAssetFinder
{
    /// <summary>
    /// Finds and loads the first asset matching the given name and type.
    /// Works regardless of folder location, as long as the asset name is preserved.
    /// </summary>
    public static T Find<T>(string name) where T : Object
    {
        string filter = name + " t:" + typeof(T).Name;
        string[] guids = AssetDatabase.FindAssets(filter);

        if (guids.Length == 0)
        {
            Debug.LogError("Asset not found: " + filter);
            return null;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }
}
#endif