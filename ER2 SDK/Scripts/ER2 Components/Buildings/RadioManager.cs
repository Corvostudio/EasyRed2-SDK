using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class RadioManager : MonoBehaviour
{
    [Range(1,100)]
    public float radioRadius = 10;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioRadius);
    }
}
