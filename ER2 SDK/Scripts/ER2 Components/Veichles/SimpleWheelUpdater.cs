using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public partial class SimpleWheelUpdater : MonoBehaviour
{
    [Tooltip("Vehicle whose speed and steering drive these wheels.")]
    public Vehicle vehicle;

    [Tooltip("Maximum steering angle of these wheels, in degrees. 0 = they do not steer.")]
    [Range(-70, 70)]
    public float steer_angle = 0;
    [Tooltip("Radius of the wheels, in metres: sets how fast they spin.")]
    public float wheel_radius = .3f;

    [Tooltip("Wheel models rotated by this component.")]
    public Transform[] wheelsTransform_syncOnlyRot;

}