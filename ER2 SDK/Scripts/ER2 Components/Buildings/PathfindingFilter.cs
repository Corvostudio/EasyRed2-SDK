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
    [Tooltip("Unit types that can NOT create path nodes on top of this collider's surface")]
    [SerializeField] public PathfindingIgnoreFlags ignoreFlags = PathfindingIgnoreFlags.None;

    [Tooltip("Unit types that can path THROUGH this collider as if it didn't exist (e.g. vehicles that can smash through fences or light obstacles). Opposite of ignoreFlags: the obstacle no longer blocks their path edges, and its surface hosts no nodes for them.")]
    [SerializeField] public PathfindingIgnoreFlags passThroughFlags = PathfindingIgnoreFlags.None;
}
