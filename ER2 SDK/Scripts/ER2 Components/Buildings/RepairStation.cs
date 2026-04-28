using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public partial class RepairStation : MonoBehaviour
{
    [Range(0.1f, 5)]
    public float radius = 1.5f;

    void OnDrawGizmosSelected()
    {
        // Display the explosion radius when selected
        Gizmos.color = new Color(1, 1, 0, 0.2F);
        Gizmos.DrawSphere(transform.position, radius);
    }
}
