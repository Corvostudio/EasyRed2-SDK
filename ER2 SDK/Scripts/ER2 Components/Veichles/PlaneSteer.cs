using UnityEditor;
using UnityEngine;

public partial class PlaneSteer : MonoBehaviour
{
    [Tooltip("Defines the plane connected to this steer control bar")]
    public VehiclePlane connectedPlane;

    [Tooltip("The multiplier rotation of the steer bar on plane turn")]
    [Range(-2, 2)]
    public float rotationMultiplier = .05f;

    public Seats GetDriverSeat()
    {
        if (!connectedPlane || connectedPlane.seats == null)
            return null;

        foreach (Seats seat in connectedPlane.seats)
        {
            if (seat != null && seat.isDriver)
                return seat;
        }

        return null;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlaneSteer))]
public class PlaneSteerEditor : Editor
{
    private float testPitch;
    private float testRoll;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlaneSteer steer = (PlaneSteer)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rotation Test", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        testPitch = EditorGUILayout.Slider("Test Pitch", testPitch, -45f, 45f);
        testRoll = EditorGUILayout.Slider("Test Roll", testRoll, -45f, 45f);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(steer.transform, "Preview Plane Steer Rotation");

            steer.transform.localRotation = Quaternion.Euler(
                testPitch,
                0f,
                testRoll
            );

            EditorUtility.SetDirty(steer.transform);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Reset Test Rotation"))
        {
            Undo.RecordObject(steer.transform, "Reset Plane Steer Rotation");

            steer.transform.localRotation = Quaternion.identity;
            testPitch = 0f;
            testRoll = 0f;

            EditorUtility.SetDirty(steer.transform);
            SceneView.RepaintAll();
        }


        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hand IK Setup", EditorStyles.boldLabel);

        if (GUILayout.Button("Create / Assign Driver Hand IK"))
        {
            CreateAndAssignDriverHandIK(steer);
        }

        if (GUILayout.Button("Enable / Disable hands placement checker") && !Application.isPlaying)
        {
            var existing = Resources.FindObjectsOfTypeAll<HandsPlacementCheckerPlaneSteer>();
            HandsPlacementCheckerPlaneSteer mine = null;
            foreach (var c in existing)
            {
                if (c == null || EditorUtility.IsPersistent(c)) continue;
                if (c.attached_ps == steer) { mine = c; continue; }
                DestroyImmediate(c.gameObject);
            }

            if (mine != null)
            {
                DestroyImmediate(mine.gameObject);
            }
            else
            {
                var checker = Instantiate(EditorAssetFinder.Find<GameObject>("HandsPlacementChecker_PlaneSteer"))
                    .GetComponent<HandsPlacementCheckerPlaneSteer>();
                checker.attached_ps = steer;
            }
        }
    }

    private static Transform CreateTesterHand(Transform parent, string name)
    {
        GameObject hand = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        Undo.RegisterCreatedObjectUndo(hand, "Create Tester Hand");

        hand.name = name;
        hand.transform.SetParent(parent);
        hand.transform.localPosition = Vector3.zero;
        hand.transform.localRotation = Quaternion.identity;
        hand.transform.localScale = Vector3.one * 0.08f;

        Collider collider = hand.GetComponent<Collider>();
        if (collider)
            Object.DestroyImmediate(collider);

        return hand.transform;
    }

    private static void CreateAndAssignDriverHandIK(PlaneSteer steer)
    {
        if (!steer || !steer.connectedPlane)
        {
            Debug.LogWarning("PlaneSteer has no connected plane.");
            return;
        }

        Seats driverSeat = GetDriverSeat(steer.connectedPlane);

        if (driverSeat == null)
        {
            Debug.LogWarning("Connected plane has no driver seat.");
            return;
        }

        Undo.RecordObject(steer.connectedPlane, "Assign PlaneSteer Hand IK");

        if (!driverSeat.lefthandIsIK)
        {
            Transform leftIK = FindOrCreateChild(
                steer.transform,
                "Left Hand IK",
                new Vector3(-0.18f, 0f, 0f)
            );

            driverSeat.lefthandIsIK = leftIK;
        }

        if (!driverSeat.rightHandIK)
        {
            Transform rightIK = FindOrCreateChild(
                steer.transform,
                "Right Hand IK",
                new Vector3(0.18f, 0f, 0f)
            );

            driverSeat.rightHandIK = rightIK;
        }

        EditorUtility.SetDirty(steer.connectedPlane);
        SceneView.RepaintAll();
    }

    private static Seats GetDriverSeat(VehiclePlane plane)
    {
        if (!plane || plane.seats == null)
            return null;

        foreach (Seats seat in plane.seats)
        {
            if (seat != null && seat.isDriver)
                return seat;
        }

        return null;
    }

    private static Transform FindOrCreateChild(
        Transform parent,
        string childName,
        Vector3 localPosition
    )
    {
        Transform existing = parent.Find(childName);

        if (existing)
            return existing;

        GameObject go = new GameObject(childName);

        Undo.RegisterCreatedObjectUndo(go, "Create PlaneSteer Hand IK");

        go.transform.SetParent(parent);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        return go.transform;
    }
}
#endif