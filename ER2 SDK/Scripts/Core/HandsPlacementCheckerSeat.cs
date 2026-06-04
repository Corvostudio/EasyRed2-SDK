using UnityEngine;

[ExecuteInEditMode]
public class HandsPlacementCheckerSeat : MonoBehaviour
{
    public VehicleSeat attached_seat;

    public Transform left_hand;
    public Transform right_hand;

    private void Update()
    {
        if (left_hand && right_hand && attached_seat && transform.parent == null)
        {
            Vehicle veh = attached_seat.GetComponentInParent<Vehicle>(true); // any depth, incl. inactive

            if (veh != null && veh.seats != null)
            {
                foreach (Seats s in veh.seats)
                {
                    if (s != null && s.seat == attached_seat && s.lefthandIsIK != null && s.rightHandIK != null)
                    {
                        right_hand.position = s.rightHandIK.position;
                        right_hand.rotation = s.rightHandIK.rotation;

                        left_hand.position = s.lefthandIsIK.position;
                        left_hand.rotation = s.lefthandIsIK.rotation;

                        return;
                    }
                }
            }

            Debug.LogWarning("HandsPlacementCheckerSeat: questa seat non è registrata in un Vehicle padre, oppure non ha gli IK (left/right) assegnati.");
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