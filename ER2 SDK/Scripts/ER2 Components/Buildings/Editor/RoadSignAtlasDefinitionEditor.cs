#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadSignAtlasDefinition))]
public sealed class RoadSignAtlasDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        RoadSignAtlasDefinition atlas = (RoadSignAtlasDefinition)target;

        EditorGUI.BeginChangeCheck();

        int columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", atlas.columns));
        int rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", atlas.rows));
        bool indexFromTopLeft = EditorGUILayout.Toggle("Index From Top Left", atlas.indexFromTopLeft);
        float paddingUv = Mathf.Max(0f, EditorGUILayout.FloatField("Padding UV", atlas.paddingUv));

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(atlas, "Edit Road Sign Atlas Definition");
            atlas.columns = columns;
            atlas.rows = rows;
            atlas.indexFromTopLeft = indexFromTopLeft;
            atlas.paddingUv = paddingUv;
            atlas.EnsureSlotCount();
            MarkAssetDirty(atlas);
        }

        atlas.EnsureSlotCount();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Slot Names", EditorStyles.boldLabel);

        float prevLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 26; // solo il numero; il resto va al TextField

        for (int row = 0; row < atlas.Rows; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < atlas.Columns; col++)
            {
                int index = row * atlas.Columns + col;
                string currentName = atlas.GetSlotName(index);

                EditorGUI.BeginChangeCheck();
                string nextName = EditorGUILayout.TextField($"{index:00}", currentName);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(atlas, "Rename Road Sign Atlas Slot");
                    atlas.SetSlotName(index, nextName);
                    MarkAssetDirty(atlas);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUIUtility.labelWidth = prevLabelWidth;
    }

    private static void MarkAssetDirty(Object asset)
    {
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssetIfDirty(asset);
    }
}

[CustomEditor(typeof(RoadSignUvBaker))]
public sealed class RoadSignUvBakerEditor : Editor
{
    private RoadSignUvBaker baker;

    private void OnEnable() { baker = (RoadSignUvBaker)target; }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        RoadSignAtlasDefinition atlasDefinition = (RoadSignAtlasDefinition)EditorGUILayout.ObjectField(
            "Atlas Definition", baker.atlasDefinition, typeof(RoadSignAtlasDefinition), false);
        bool applyOnEnable = EditorGUILayout.Toggle("Apply On Enable", baker.applyOnEnable);
        bool autoApplyInEditor = EditorGUILayout.Toggle("Auto Apply In Editor", baker.autoApplyInEditor);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(baker, "Edit Road Sign UV Baker");
            baker.atlasDefinition = atlasDefinition;
            baker.applyOnEnable = applyOnEnable;
            baker.autoApplyInEditor = autoApplyInEditor;
            MarkComponentDirty(baker);
        }

        if (baker.atlasDefinition == null)
            EditorGUILayout.HelpBox("Assign an Atlas Definition.", MessageType.Warning);

        EditorGUILayout.Space(8);

        if (GUILayout.Button("Add Target"))
        {
            Undo.RecordObject(baker, "Add Road Sign Target");
            baker.targets.Add(new RoadSignUvBaker.SignTarget());
            MarkComponentDirty(baker);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply All (preview)")) baker.ApplyAll();
        if (GUILayout.Button("Clear")) baker.ClearAll();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        string[] atlasLabels = baker.atlasDefinition != null
            ? baker.atlasDefinition.GetPopupLabels()
            : new[] { "Missing Atlas Definition" };

        if (baker.targets == null) return;

        for (int i = 0; i < baker.targets.Count; i++)
        {
            RoadSignUvBaker.SignTarget target = baker.targets[i];
            if (target == null) continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Target {i}", EditorStyles.boldLabel);
            if (GUILayout.Button("Apply", GUILayout.Width(60))) baker.ApplyTarget(i);
            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                Undo.RecordObject(baker, "Remove Road Sign Target");
                baker.targets.RemoveAt(i);
                MarkComponentDirty(baker);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();

            Renderer rend = (Renderer)EditorGUILayout.ObjectField("Renderer", target.targetRenderer, typeof(Renderer), true);

            int atlasSlotIndex = target.atlasSlotIndex;
            if (baker.atlasDefinition != null)
            {
                atlasSlotIndex = Mathf.Clamp(atlasSlotIndex, 0, baker.atlasDefinition.SlotCount - 1);
                atlasSlotIndex = EditorGUILayout.Popup("Default Slot", atlasSlotIndex, atlasLabels);
            }
            else EditorGUILayout.Popup("Default Slot", 0, atlasLabels);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(baker, "Edit Road Sign Target");
                target.targetRenderer = rend;
                target.atlasSlotIndex = atlasSlotIndex;
                MarkComponentDirty(baker);
                if (baker.autoApplyInEditor) baker.ApplyTarget(i);
            }

            if (target.targetRenderer == null)
                EditorGUILayout.HelpBox("Assegna il Renderer della faccia del cartello.", MessageType.Warning);

            EditorGUILayout.EndVertical();
        }
    }

    private static void MarkComponentDirty(Component component)
    {
        EditorUtility.SetDirty(component);
        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }
}

#endif