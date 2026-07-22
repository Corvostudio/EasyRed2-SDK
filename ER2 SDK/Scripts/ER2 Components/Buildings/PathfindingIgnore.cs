using UnityEditor;
using UnityEngine;
using static PathfindingIgnore;

[ExecuteInEditMode]
public partial class PathfindingIgnore : MonoBehaviour
{
    public IgnoreLayer ignoreLayer = IgnoreLayer.all;

    void Start()
    {
        //pf.RegisterFilter();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (!GetComponent<PathfindingFilter>())
            {
                AddNewVersion();
                EditorUtility.SetDirty(gameObject);
                DestroyImmediate(this);
            }
            return;
        }
#endif
        AddNewVersion();
        Destroy(this);
    }

    private void AddNewVersion()
    {
        PathfindingFilter pf = gameObject.AddComponent<PathfindingFilter>();
        switch (ignoreLayer)
        {
            case IgnoreLayer.all:
                pf.ignoreFlags = PathfindingIgnoreFlags.All;
                break;
            case IgnoreLayer.tanks:
                pf.ignoreFlags = PathfindingIgnoreFlags.Vehicles;
                break;
            case IgnoreLayer.units:
                pf.ignoreFlags = PathfindingIgnoreFlags.Infantry;
                break;
        }
    }
}

public enum IgnoreLayer
{
    all,
    tanks,
    units
}