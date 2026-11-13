using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public partial class HandheldItem : ItemObject
{
    [Tooltip("IK target of the left hand on the item (the fore grip of a weapon).")]
    public Transform leftHandHoldPosition;
    [Tooltip("Third-person settings: animator controller, left hand IK and placement in the hands and on the back.")]
    public HandHeldAnims tpsAnims = new HandHeldAnims();


    [System.Serializable]
    public partial class HandHeldAnims
    {
        [Tooltip("Third-person animator controller used while holding this item (e.g. CCV2_rifle).")]
        public string userController = "CCV2_rifle";
        [Tooltip("Keep the left hand on its IK target while sprinting.")]
        public bool IKWhenSprinting = true;
        [Tooltip("Offset of the item in the right hand. For small adjustments only.")]
        public Vector3 localPosInHands = Vector3.zero;

        [Tooltip("Scale of the item in the hands. 1 = unchanged.")]
        public float localScaleInHands = 1;//rescale in case of wrong scale
        [Tooltip("Offset of the item when stowed on the back or in the holster.")]
        public Vector3 localPosOnBack = Vector3.zero;
        [Tooltip("Rotation (euler angles) of the item when stowed on the back or in the holster.")]
        public Vector3 localRotOnBack = Vector3.zero;
    }
}