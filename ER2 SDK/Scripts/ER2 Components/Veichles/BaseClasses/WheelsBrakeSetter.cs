using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Forza wheel braking ad inizio gioco
public partial class WheelsBrakeSetter : MonoBehaviour
{
    [Tooltip("Wheels that are braked when the game starts.")]
    public WheelCollider[] wheels;
    [Tooltip("Brake torque applied to those wheels.")]
    public float brakePower = 300;

}
