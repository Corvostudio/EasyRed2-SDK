using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public partial class DoorRegister : MapPropIndexable //MapPropIndexable
{
    [Tooltip("Defines all the doors the player can interact. NOTE: Press \"Refresh door register\" after you inserted all the doors for the building")]
    public InteragibleDoor[] registeredDoors;

}

#if UNITY_EDITOR
[CustomEditor(typeof(DoorRegister))]
public class DoorRegisterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DoorRegister myScript = (DoorRegister)target;
        
        if (GUILayout.Button("Refresh door register"))
        {
            //myScript.building_id = myScript.gameObject.name;
            myScript.registeredDoors = myScript.GetComponentsInChildren<InteragibleDoor>();
        }
    }
}
#endif