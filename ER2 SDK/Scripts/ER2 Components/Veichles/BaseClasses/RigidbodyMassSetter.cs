using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//forza posizione massa ad inizio gioco
public partial class RigidbodyMassSetter : MonoBehaviour
{
    public Vector3 localMassPos;

    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.TransformPoint(localMassPos), 1);
    }
}
