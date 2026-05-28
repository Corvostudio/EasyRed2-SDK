using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RandomDetailEnabler : MonoBehaviour
{
    [System.Serializable]
    public class DetailGroup
    {
        public GameObject[] objects;
        [Range(0f, 100f)]
        public float spawnProbability = 50f;
    }

    public DetailGroup[] groups;

    // --- Legacy fields, conservati solo per auto-upgrade dei prefab vecchi ---
    [HideInInspector, SerializeField] private GameObject[] details;
    [HideInInspector, SerializeField] private float[] spawnProbabilities;
    [HideInInspector, SerializeField] private bool migrated;
    // --------------------------------------------------------------------------

    public void MigrateIfNeeded()
    {
        if (migrated) return;

        if (details != null && details.Length > 0)
        {
            DetailGroup[] newGroups = new DetailGroup[details.Length];
            for (int i = 0; i < details.Length; i++)
            {
                newGroups[i] = new DetailGroup
                {
                    objects = new GameObject[] { details[i] },
                    spawnProbability = (spawnProbabilities != null && i < spawnProbabilities.Length)
                        ? spawnProbabilities[i] : 50f
                };
            }

            // Se c'erano già dei gruppi nuovi, accoda invece di sovrascrivere
            if (groups != null && groups.Length > 0)
            {
                DetailGroup[] merged = new DetailGroup[groups.Length + newGroups.Length];
                System.Array.Copy(groups, 0, merged, 0, groups.Length);
                System.Array.Copy(newGroups, 0, merged, groups.Length, newGroups.Length);
                groups = merged;
            }
            else
            {
                groups = newGroups;
            }

            details = null;
            spawnProbabilities = null;
        }

        migrated = true;
    }

    void OnValidate() { MigrateIfNeeded(); }
    void Awake() { MigrateIfNeeded(); }

    void Start()
    {
        Randomize();
    }

    public void Randomize()
    {
        MigrateIfNeeded();
        if (groups == null) return;

        for (int i = 0; i < groups.Length; i++)
        {
            DetailGroup g = groups[i];
            if (g == null || g.objects == null) continue;

            bool active = Random.Range(0f, 100f) < g.spawnProbability;
            for (int j = 0; j < g.objects.Length; j++)
            {
                if (g.objects[j] != null)
                    g.objects[j].SetActive(active);
            }
        }
    }

    public void ResetAll()
    {
        if (groups == null) return;
        foreach (DetailGroup g in groups)
        {
            if (g == null || g.objects == null) continue;
            foreach (GameObject go in g.objects)
                if (go != null) go.SetActive(true);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(RandomDetailEnabler))]
[CanEditMultipleObjects]
public class RandomDetailEnablerEditor : Editor
{
    SerializedProperty groupsProp;

    void OnEnable()
    {
        // Forza la migrazione alla prima selezione
        foreach (Object t in targets)
        {
            RandomDetailEnabler e = (RandomDetailEnabler)t;
            e.MigrateIfNeeded();
            EditorUtility.SetDirty(e);
        }

        groupsProp = serializedObject.FindProperty("groups");
        groupsProp.isExpanded = true;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        groupsProp.isExpanded = EditorGUILayout.Foldout(groupsProp.isExpanded, "Detail Groups", true);

        if (groupsProp.isExpanded)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(groupsProp.FindPropertyRelative("Array.size"));

            for (int i = 0; i < groupsProp.arraySize; i++)
            {
                SerializedProperty groupProp = groupsProp.GetArrayElementAtIndex(i);
                SerializedProperty objectsProp = groupProp.FindPropertyRelative("objects");
                SerializedProperty probProp = groupProp.FindPropertyRelative("spawnProbability");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                string groupLabel = $"EMPTY {i}";
                if (objectsProp.arraySize > 0)
                {
                    SerializedProperty firstObj = objectsProp.GetArrayElementAtIndex(0);
                    if (firstObj.objectReferenceValue != null)
                        groupLabel = firstObj.objectReferenceValue.name;
                }
                objectsProp.isExpanded = EditorGUILayout.Foldout(objectsProp.isExpanded, groupLabel, true);
                probProp.floatValue = EditorGUILayout.Slider(probProp.floatValue, 0f, 100f, GUILayout.Width(160));
                EditorGUILayout.LabelField("%", GUILayout.Width(18));
                EditorGUILayout.EndHorizontal();

                if (objectsProp.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(objectsProp, new GUIContent("Objects"), true);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(10);

        if (GUILayout.Button("Randomize Details"))
        {
            foreach (Object t in targets)
            {
                RandomDetailEnabler e = (RandomDetailEnabler)t;
                e.Randomize();
                EditorUtility.SetDirty(e);
            }
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Reset Details (Enable All)"))
        {
            foreach (Object t in targets)
            {
                RandomDetailEnabler e = (RandomDetailEnabler)t;
                e.ResetAll();
                EditorUtility.SetDirty(e);
            }
        }

        if (GUILayout.Button("Set All to 50%"))
        {
            foreach (Object t in targets)
            {
                RandomDetailEnabler e = (RandomDetailEnabler)t;
                if (e.groups != null)
                    foreach (var g in e.groups)
                        if (g != null) g.spawnProbability = 50f;
                EditorUtility.SetDirty(e);
            }
        }

        EditorGUILayout.EndHorizontal();
    }
}
#endif