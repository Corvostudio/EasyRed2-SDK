using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEngine;

public partial class Turret : MonoBehaviour
{
    [Tooltip("Max angle of rotation (left/right) of the turret. If 180, the turret can rotate 360")]
    [Range(0, 180)]
    public int yAxisBond;
    [Tooltip("Axis pivot for the Y rotation of the turret")]
    public GameObject yAxis;

    [Tooltip("Maximum angle of elevation of the gun on the turret")]
    [Range(-89, 90)]
    public int xAxisBond;
    [Tooltip("Maximum value of depression of the gun of the turret")]
    [Range(-89, 90)]
    public int xAxisBondDown = 10;
    [Tooltip("Pivot of elevation of the gun")]
    public GameObject xAxis;
    [Tooltip("Angle of standard zeroing of the gun. if very high (over 45°) it will behave as mortar.")]
    [Range(0, 90)]
    public float zeroingAngle = 0;

    [Tooltip("Look position of the camera")]
    public Transform lookPosition;

    [Tooltip("FOV when aming")]
    [Range(15, 65)]
    public float scopeFOV = 25;
    [Tooltip("Position of the camera when aiming")]
    public Transform scopePosition;
    [Tooltip("Sprites of the scope when aiming")]
    public string scopeSprite_id="tank_scope_2";

    [Tooltip("Rotation speed of the turret")]
    [Range(0, 100)]
    public float rotationSpeed = 8;
    [Tooltip("Camera rotates independently from the turret. Usefull if there is a turret parented to another turret")]
    public bool independentLookRotation = false;//camera ruota indipendentemente da mg

    [Tooltip("When true it will prioritize attacking planes with any of it's weapons. However for the case of bombers, it will prioritize them anyway if the gun has Time Fuse Flak ammos.")]
    public bool isAA = false;//when isAA=true AI will target planes first, other later, when isAA=false AI will not target planes at all
    [Tooltip("Rotation turn sound")]
    public AudioClip turnSoundLoop_outside;

}