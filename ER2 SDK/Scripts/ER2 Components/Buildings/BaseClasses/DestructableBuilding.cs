using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class DestructableBuilding : MonoBehaviour, Damagable
{
    [Tooltip("Set full life of the destructible building part")]
    [Range(0, 5000)]
    public float life = 100;

    [Tooltip("Defines minimum bullet penetration value to damage this part")]
    [Range(0, 200)]
    public float minBulletPenetration = 0;
    [Tooltip("Defines the damage multiplier for the damage applied by the bullet")]
    [Range(0, 5)]
    public float bulletDamageMultiplier = 1;
    [Tooltip("Defines the damage multiplier for the damage applied by fire")]
    [Range(0, 5)]
    public float fireDamageMultiplier = 1;
    [Tooltip("Defines the damage multiplier for the damage applied by explosion force")]
    [Range(0, 5)]
    public float explosionDamageMultiplier = 1;
    [Tooltip("Defines the damage multiplier for the damage applied by vehicle collision")]
    [Range(0, 5)]
    public float vehicleCollisionMultiplier = 1;

    [Header("Instantiate prop effect (Particle, animation..) when the phase changes")]
    public Vector3 dstVfxPosition = Vector3.zero;

    [Tooltip("This effect is spawned from it's ID when the phase changes. Particle effects will be deleted after particle animation is complete.")]
    public string onPhaseChangeEffect = "";
    [Tooltip("Name of the bundle that contains the particle effect prefab")]
    public string fxBundle = "er2fxs";
}