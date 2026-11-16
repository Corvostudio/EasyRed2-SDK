using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class DestructableBuilding : MonoBehaviour, Damagable
{
    [Tooltip("Life of this part. It is destroyed when the life reaches zero.")]
    [Range(0, 5000)]
    public float life = 100;

    [Header("Damage")]
    [Tooltip("Projectiles damage this part only if their armour penetration, in mm, is higher than this.")]
    [Range(0, 200)]
    public float minBulletPenetration = 0;
    [Tooltip("Multiplier of the damage from projectiles.")]
    [Range(0, 5)]
    public float bulletDamageMultiplier = 1;
    [Tooltip("Multiplier of the damage from fire.")]
    [Range(0, 5)]
    public float fireDamageMultiplier = 1;
    [Tooltip("Multiplier of the damage from explosions.")]
    [Range(0, 5)]
    public float explosionDamageMultiplier = 1;
    [Tooltip("Multiplier of the damage from vehicle collisions.")]
    [Range(0, 5)]
    public float vehicleCollisionMultiplier = 1;

    [Header("Phase change effect")]
    [Tooltip("Prefab name of the effect spawned when the part changes phase or is destroyed. Empty = none.")]
    public string onPhaseChangeEffect = "";
    [Tooltip("Bundle the effect is loaded from.")]
    public string fxBundle = "er2fxs";
    [Tooltip("Local position where the effect is spawned.")]
    public Vector3 dstVfxPosition = Vector3.zero;

#if UNITY_EDITOR
    //Added in the editor to a prop that already has its Destructible Manager: the part goes on the list of the manager at once. On a
    //prefab picked in the Project window nothing else puts it there, and a part the manager does not list is neither saved nor synced.
    private void Reset()
    {
        DestructibleManager manager = GetComponentInParent<DestructibleManager>(true);
        if (!manager || System.Array.IndexOf(manager.destructibleParts, this) >= 0)
            return;
        Undo.RecordObject(manager, "Add destructible part");
        ArrayUtility.Add(ref manager.destructibleParts, this);
        EditorUtility.SetDirty(manager);
    }
#endif
}