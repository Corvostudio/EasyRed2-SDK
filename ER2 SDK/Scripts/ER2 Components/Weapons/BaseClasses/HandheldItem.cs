using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public partial class HandheldItem : ItemObject
{
    [Tooltip("IK Target transform for left hand position on the forward handle")]
    public Transform leftHandHoldPosition;
    [Tooltip("TPS anim data")]
    public HandHeldAnims tpsAnims = new HandHeldAnims();


    [System.Serializable]
    public partial class HandHeldAnims
    {
        [Tooltip("Character controller for TPS anim")]
        public string userController = "CCV2_rifle";
        [Tooltip("Enable/disable left hand IK when sprinting")]
        public bool IKWhenSprinting = true;
        //[Tooltip("Sprint anim type when run")]
        //public FPSAnimationSetID fpsAnimSet = FPSAnimationSetID.rifleRun;
        [Tooltip("Shift weapon local position on right hand. NOTICE: This should be used only for small adjustments!! (optional)")]
        public Vector3 localPosInHands = Vector3.zero;

        [Tooltip("Default to 1")]
        public float localScaleInHands = 1;//rescale in case of wrong scale
    }

    [Obsolete]
    public enum FPSAnimationSetID
    {
        rifleRun = 0,
        pistolRun = 1,
        germanRifleRun = 2,
        japaneseRifleRun = 3,
        bazooka = 4
    }
}