using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class MGConvergence : MonoBehaviour
{
    [Tooltip("MG(s) to adjust convergence to")]
    public Transform[] MGs;

    [Tooltip("Distance of convergence")]
    [Range(100, 5000)]
    public float convergenceDistance = 230;

    [Tooltip("Up/Down shift of the convergence relative to this plane")]
    [Range(-3, 3)]
    public float convergence_upDown = .5f;
   
}
