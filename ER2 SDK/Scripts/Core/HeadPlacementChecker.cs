using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HeadPlacementChecker : MonoBehaviour
{
    public ItemHelmet attached_headgear;

    private void Update()
    {
        if (attached_headgear && transform.parent==null)
        {
            transform.localScale = Vector3.one;
            transform.position = attached_headgear.transform.position;
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }
}
