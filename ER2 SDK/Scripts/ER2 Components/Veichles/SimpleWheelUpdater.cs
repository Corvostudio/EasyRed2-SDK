using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public partial class SimpleWheelUpdater : MonoBehaviour
{
    public Vehicle vehicle;

    [Range(-70, 70)]
    public float steer_angle = 0;
    public float wheel_radius = .3f;

    public Transform[] wheelsTransform_syncOnlyRot;

}