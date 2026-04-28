using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public partial class VehicleDamageableWheelPart : VehicleDamagablePart
{
    public WheelCollider connectedWheel;

    protected new void Awake()
    {
        base.Awake();

        if (connectedWheel)
        {
#if !UNITY_EDITOR
            if (Application.isPlaying)
                originalWheelRadius = connectedWheel.radius;
#else
            originalWheelRadius = connectedWheel.radius;
#endif
        }
    }

#if UNITY_EDITOR
    public void AssignDestructibleWheel(WheelCollider wc)
    {
        if (wc)
        {
            //Debug.Log("Parameters resetted on wheel!", gameObject);
            impactType = ImpactType.generic;
            armorThickness = 0;
            considerAngledArmor = false;
            canRicochet = false;
            damagePlayerOnCollision = false;
            damageHullOnGettingRammed = false;
            connectedWheel = wc;

            if (!GetComponent<BoxCollider>())
            {
                BoxCollider bc = gameObject.AddComponent<BoxCollider>();
                bc.isTrigger = true;
                bc.size = new Vector3(0.15f, wc.radius * 1.8f, wc.radius * 1.8f) / transform.lossyScale.x;
                //bc.center = new Vector3(0, -wc.suspensionDistance / 2, 0);
            }
        }
    }
#endif

}
