#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class TPSRigTesterAutoSelection
{
    private const string EnabledKey = "ER2.TPSRigTester.AutoSelection.Enabled";
    private const string UniformSleeveKey = "ER2.TPSRigTester.AutoSelection.UniformSleeve";

    private static int s_lastLinkedComponentId;
    private static bool s_checkQueued;

    public enum AutoUniformSleeveMode
    {
        LongSleeve,
        ShortSleeveIfAvailable
    }

    static TPSRigTesterAutoSelection()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        Selection.selectionChanged += OnSelectionChanged;

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
    }

    public static bool Enabled
    {
        get => EditorPrefs.GetBool(EnabledKey, false);
        set
        {
            if (Enabled == value) return;

            EditorPrefs.SetBool(EnabledKey, value);
            s_lastLinkedComponentId = 0;

            if (value)
                QueueSelectionCheck();
        }
    }

    public static AutoUniformSleeveMode UniformSleeveMode
    {
        get => (AutoUniformSleeveMode)EditorPrefs.GetInt(UniformSleeveKey, (int)AutoUniformSleeveMode.LongSleeve);
        set => EditorPrefs.SetInt(UniformSleeveKey, (int)value);
    }

    public static bool DrawInspectorUI()
    {
        GUILayout.Space(4);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        bool enabled = EditorGUILayout.ToggleLeft("Auto-test selected wearable", Enabled);
        if (enabled != Enabled)
            Enabled = enabled;

        using (new EditorGUI.DisabledScope(!Enabled))
        {
            AutoUniformSleeveMode sleeveMode =
                (AutoUniformSleeveMode)EditorGUILayout.EnumPopup("Uniform auto mode", UniformSleeveMode);

            if (sleeveMode != UniformSleeveMode)
                UniformSleeveMode = sleeveMode;
        }

        EditorGUILayout.EndVertical();

        return enabled;
    }

    private static void OnSelectionChanged()
    {
        if (!Enabled) return;
        QueueSelectionCheck();
    }

    private static void QueueSelectionCheck()
    {
        if (s_checkQueued) return;

        s_checkQueued = true;
        EditorApplication.delayCall += ProcessSelectionDelayed;
    }

    private static void ProcessSelectionDelayed()
    {
        s_checkQueued = false;

        if (!Enabled) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        ItemHelmet helmet = ResolveActiveComponent<ItemHelmet>();
        if (helmet != null)
        {
            AutoLinkHelmet(helmet);
            return;
        }

        ItemClothing clothing = ResolveActiveComponent<ItemClothing>();
        if (clothing != null)
            AutoLinkClothing(clothing);
    }

    private static T ResolveActiveComponent<T>() where T : Component
    {
        Object selected = Selection.activeObject;
        if (selected == null) return null;

        if (selected is T directComponent)
            return directComponent;

        if (selected is Component component)
            return component.GetComponent<T>();

        if (selected is GameObject gameObject)
            return gameObject.GetComponent<T>();

        string path = AssetDatabase.GetAssetPath(selected);
        if (!string.IsNullOrEmpty(path))
        {
            GameObject assetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (assetRoot != null)
                return assetRoot.GetComponent<T>();
        }

        return null;
    }

    private static void AutoLinkClothing(ItemClothing clothing)
    {
        if (clothing == null) return;

        int id = clothing.GetInstanceID();
        if (id == s_lastLinkedComponentId && TPSRigTesterManager.FindTester() != null)
            return;

        s_lastLinkedComponentId = id;

        if (clothing.type == WearableType.uniform)
        {
            SleeveMode sleeveMode =
                UniformSleeveMode == AutoUniformSleeveMode.ShortSleeveIfAvailable &&
                clothing.mesh_short_sleeve != null
                    ? SleeveMode.rolled
                    : SleeveMode.unrolled;

            TPSRigTesterManager.LinkUniform(clothing, sleeveMode);
        }
        else
        {
            TPSRigTesterManager.LinkGear(clothing);
        }
    }

    private static void AutoLinkHelmet(ItemHelmet helmet)
    {
        if (helmet == null) return;

        int id = helmet.GetInstanceID();
        if (id == s_lastLinkedComponentId && TPSRigTesterManager.FindTester() != null)
            return;

        s_lastLinkedComponentId = id;
        TPSRigTesterManager.LinkHelmet(helmet);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        s_lastLinkedComponentId = 0;
        s_checkQueued = false;
    }

    private static void OnBeforeAssemblyReload()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
    }
}
#endif