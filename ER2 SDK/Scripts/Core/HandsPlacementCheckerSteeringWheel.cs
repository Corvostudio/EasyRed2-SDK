using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HandsPlacementCheckerSteeringWheel : MonoBehaviour
{
    public SteeringWheel attached_sw;

    public Transform left_hand;
    public Transform right_hand;

    private void Update()
    {
        if (left_hand && right_hand && attached_sw && attached_sw.connectedVeh && transform.parent==null)
        {
            foreach(Seats s in attached_sw.connectedVeh.seats)
            {
                if (s.isDriver && s.lefthandIsIK!=null && s.rightHandIK!=null)
                {
                    right_hand.transform.position = s.rightHandIK.position;
                    right_hand.transform.rotation = s.rightHandIK.rotation;

                    left_hand.transform.position = s.lefthandIsIK.position;
                    left_hand.transform.rotation = s.lefthandIsIK.rotation;

                    return;
                }
            }
            Debug.LogWarning("Missing driver seat or Left/Right hand IK position on driver seat");
        }

        DestroyImmediate(gameObject);
    }
    private void OnEnable()
    {
        var flags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.NotEditable;
        foreach (var t in GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = flags;

#if UNITY_EDITOR
        UnityEditor.SceneVisibilityManager.instance.DisablePicking(gameObject, true);
#endif
    }
}
