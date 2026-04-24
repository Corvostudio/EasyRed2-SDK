using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Forza wheel braking ad inizio gioco
public partial class WheelsBrakeSetter : MonoBehaviour
{
    [Header("Used normally for gliders: Wheels that brakes when the glider slows down")]
    public WheelCollider[] wheels;
    public float brakePower = 300;

}
