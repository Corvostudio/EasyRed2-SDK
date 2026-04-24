using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class SteeringWheel : MonoBehaviour
{
    [Tooltip("Vehicle connected to this steering wheel")]
    public MovableVehicle connectedVeh;


    [Tooltip("Axis where to rotate the steering wheel around")]
    [Range(0, 2)]
    public byte axis = 0;

    [Tooltip("Multiplier of rotation of the steering wheel")]
    [Range(-90, 90)]
    public float rotationMultiplier = 45;

}


#if UNITY_EDITOR
[CustomEditor(typeof(SteeringWheel))]
public class SteeringWheelEditor : Editor
{
    public override void OnInspectorGUI()
    {

        SteeringWheel myScript = (SteeringWheel)target;

        if (GUILayout.Button("Enable / Disable hands placement checker") && !Application.isPlaying)
        {
            HandsPlacementCheckerSteeringWheel checker = GameObject.FindFirstObjectByType<HandsPlacementCheckerSteeringWheel>();
            if (checker)
            {
                if (checker.attached_sw == myScript)
                    DestroyImmediate(checker.gameObject);
                else
                    checker.attached_sw = myScript;
            }
            else
            {
                checker = Instantiate(EditorAssetFinder.Find<GameObject>("HandsPlacementChecker_SteeringWheel")).GetComponent<HandsPlacementCheckerSteeringWheel>();
                checker.attached_sw = myScript;
            }
        }


        DrawDefaultInspector();
    }
}

#endif