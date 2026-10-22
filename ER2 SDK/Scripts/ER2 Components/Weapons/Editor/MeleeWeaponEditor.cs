using UnityEditor;
// Inspector of a melee weapon: with a shared move set assigned the inline one stays hidden.
[CustomEditor(typeof(MeleeWeapon), true)]
public class MeleeWeaponEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        if (serializedObject.FindProperty("moveSetData").objectReferenceValue)
            DrawPropertiesExcluding(serializedObject, "moveSet");
        else
            DrawPropertiesExcluding(serializedObject);
        serializedObject.ApplyModifiedProperties();
    }
}
