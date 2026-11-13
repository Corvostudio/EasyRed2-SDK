using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//forza posizione massa ad inizio gioco
public partial class RigidbodyMassSetter : MonoBehaviour
{
    [Tooltip("Centre of mass of the rigidbody, in local space, set when the game starts.")]
    public Vector3 localMassPos;

    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.TransformPoint(localMassPos), 1);
    }
}
