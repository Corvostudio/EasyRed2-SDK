using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class InternalVehiclePart : MonoBehaviour
{
    [Tooltip("Type of internal damagable module")]
    public VehicleInternalPart part;

    [Tooltip("Witdh of the module")]
    [Range(0.1f, 2f)]
    public float width = .5f;
    [Tooltip("Height of the module")]
    [Range(0.1f, 2f)]
    public float height = .5f;

    [Header("Name of the destructiuon effects. Place a 'x' if this should have no effect.")]
    [Tooltip("On explosion, override the FXs (optional)")]
    public string overrideExplosionFX = "";
    [Tooltip("On damage, override the FXs (optional)")]
    public string overrideSmokeFX = "";

    public Bounds GetBounds()
    {
        return new Bounds(transform.position, new Vector3(width, height, width));
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {

        if (!Selection.gameObjects.Contains(gameObject))
            return;

        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawCube(GetBounds().center, GetBounds().size);
    }
#endif
}

public enum VehicleInternalPart
{
    engine = 0,
    hull = 1,
    fuelTank = 2,
    tracks = 3,
    wheel = 4,
}