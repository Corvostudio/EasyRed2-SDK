using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public partial class VehicleDamageableTrackPart : VehicleDamagablePart
{

    public SkinnedMeshRenderer trackMesh;

    partial void Initialize();

    protected new void Awake()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (trackMesh == null)
            {
                Debug.Log("Parameters resetted on track!", gameObject);
                impactType = ImpactType.metal;
                armorThickness = 10;
                considerAngledArmor = false;
                canRicochet = false;
                damagePlayerOnCollision = false;
                damageHullOnGettingRammed = false;

                trackMesh = GetComponent<SkinnedMeshRenderer>();

                if (trackMesh)
                {
                    BoxCollider new_bc = GetComponent<BoxCollider>();
                    if (!new_bc)
                        new_bc = gameObject.AddComponent<BoxCollider>();
                    new_bc.isTrigger = true;
                }
            }
            return;
        }
#endif

        Initialize();
    }
}
