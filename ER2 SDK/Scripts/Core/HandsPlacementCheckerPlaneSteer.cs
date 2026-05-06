using UnityEngine;

[ExecuteInEditMode]
public class HandsPlacementCheckerPlaneSteer : MonoBehaviour
{
    public PlaneSteer attached_ps;
    public Transform left_hand;
    public Transform right_hand;

    private void Update()
    {
        if (left_hand && right_hand && attached_ps && attached_ps.connectedPlane && transform.parent == null)
        {
            foreach (Seats s in attached_ps.connectedPlane.seats)
            {
                if (s != null && s.isDriver && s.lefthandIsIK != null && s.rightHandIK != null)
                {
                    right_hand.position = s.rightHandIK.position;
                    right_hand.rotation = s.rightHandIK.rotation;
                    left_hand.position = s.lefthandIsIK.position;
                    left_hand.rotation = s.lefthandIsIK.rotation;
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