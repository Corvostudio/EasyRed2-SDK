using UnityEngine;

// Bitfield enum for efficient filtering
[System.Flags]
public enum PathfindingIgnoreFlags
{
    None = 0,
    Infantry = 1 << 0,
    Vehicles = 1 << 1,
    Aircraft = 1 << 2,
    All = ~0
}

// Component-based filtering system - much faster than tags
[System.Serializable]
public partial class PathfindingFilter : MonoBehaviour
{
    [SerializeField] public PathfindingIgnoreFlags ignoreFlags = PathfindingIgnoreFlags.None;
}
