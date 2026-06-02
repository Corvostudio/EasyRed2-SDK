// ScopeCameraManager.cs — SDK side: serialized setup data only.
using UnityEngine;

[System.Serializable]
public partial class ScopeCameraManager
{
    [Tooltip("Object enabled in first person (scope lens; holds the scope camera + render material)")]
    public GameObject enableOnFPS;
}