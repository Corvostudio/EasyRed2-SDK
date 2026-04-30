// Assets/Editor/ModelImporterSkeletonFixerEditor.cs
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;

[CustomEditor(typeof(ModelImporter))]
[CanEditMultipleObjects]
public class ModelImporterSkeletonFixerEditor : Editor
{
    private Editor _internalEditor;

    private const string UseFuneePrefKey = "SkeletonFixer.UseFuneeAvatar";
    private bool _useFuneeAvatar;


    private static double next_action_time = 0;
    private static bool IsNewActionTime()
    {
        if (EditorApplication.timeSinceStartup > next_action_time)
        {
            next_action_time = EditorApplication.timeSinceStartup + .5f;
            return true;
        }
        return false;
    }

    private void OnEnable()
    {
        _useFuneeAvatar = EditorPrefs.GetBool(UseFuneePrefKey, false);

        MaterialWizardUI.Initialize();

        CreateInternalEditor();
    }

    private void OnDisable()
    {
        if (_internalEditor != null)
            DestroyImmediate(_internalEditor);
    }

    private void CreateInternalEditor()
    {
        if (_internalEditor != null)
            DestroyImmediate(_internalEditor);

        var editorType = Type.GetType("UnityEditor.ModelImporterEditor, UnityEditor");
        if (editorType == null)
        {
            Debug.LogError("ModelImporterEditor type not found. Unity internals changed?");
            return;
        }

        UnityEngine.Object context = null;
        var first = target as ModelImporter;
        if (first != null)
            context = AssetDatabase.LoadMainAssetAtPath(first.assetPath);

        Editor.CreateCachedEditorWithContext(targets, context, editorType, ref _internalEditor);
        TryDisableInternalPostProcessorsUI(_internalEditor);
    }

    public override void OnInspectorGUI()
    {
        if (_internalEditor == null)
        {
            EditorGUILayout.HelpBox("Internal ModelImporter editor missing.", MessageType.Error);
            if (GUILayout.Button("Recreate internal editor"))
                CreateInternalEditor();
            return;
        }

        _internalEditor.OnInspectorGUI();
        DrawApplyRevert();

        var tab = TryGetInternalTabIndex(_internalEditor); // commonly 0=Model,1=Rig,2=Animation,3=Materials

        if (tab == 2 || tab == -1) DrawAnimationFixUI();
        if (tab == 3 || tab == -1) MaterialWizardUI.DrawUI(targets);
    }

    private bool fps_workflow = true;
    private bool tps_workflow_loop = false;
    private void DrawAnimationFixUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("EASY RED 2 – Animation Extractor", EditorStyles.boldLabel);
        fps_workflow = EditorGUILayout.Toggle("FPS", fps_workflow);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            if (fps_workflow)
            {
                EditorGUILayout.LabelField("Extract FPS Animation", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Maya Scale: 100x"))
                        ApplyToSelected(FixKind.FPS, 100);
                    if (GUILayout.Button("Blender Scale: .01x"))
                        ApplyToSelected(FixKind.FPS, 0.01f);
                    if (GUILayout.Button("Scale: 1x"))
                        ApplyToSelected(FixKind.FPS, 1f);
                }
            }
            else
            {
                EditorGUILayout.LabelField("Set as TPS Animation", EditorStyles.boldLabel);
                tps_workflow_loop = EditorGUILayout.Toggle("Loop", tps_workflow_loop);
                if (tps_workflow_loop)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Upper body")) ApplyToSelected(FixKind.TPS_Clamp_UpperBody);
                        if (GUILayout.Button("Create mask")) ApplyToSelected(FixKind.TPS_Clamp_CreateMask);
                        if (GUILayout.Button("Default Full Body")) ApplyToSelected(FixKind.TPS_Clamp_FullBody);
                    }
                }
                else
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Upper body")) ApplyToSelected(FixKind.TPS_Loop_UpperBody);
                        if (GUILayout.Button("Create mask")) ApplyToSelected(FixKind.TPS_Loop_CreateMask);
                        if (GUILayout.Button("Default Full Body")) ApplyToSelected(FixKind.TPS_Loop_FullBody);
                    }
                }
            }
        }
    }


    private static int TryGetInternalTabIndex(Editor internalEditor)
    {
        if (internalEditor == null) return -1;

        var t = internalEditor.GetType();
        while (t != null)
        {
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(int)) continue;

                var n = f.Name;
                if (n.IndexOf("tab", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("mode", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try
                    {
                        int v = (int)f.GetValue(internalEditor);
                        if (v >= 0 && v <= 6)
                            return v;
                    }
                    catch { }
                }
            }
            t = t.BaseType;
        }

        return -1;
    }

    private void DrawApplyRevert()
    {
        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Revert", GUILayout.Width(80)))
            {
                foreach (var t in targets)
                {
                    var importer = t as ModelImporter;
                    if (!importer) continue;

                    AssetDatabase.ImportAsset(importer.assetPath, ImportAssetOptions.ForceUpdate);
                }

                CreateInternalEditor();
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("Apply", GUILayout.Width(80)))
            {
                foreach (var t in targets)
                {
                    var importer = t as ModelImporter;
                    if (!importer) continue;

                    AssetDatabase.WriteImportSettingsIfDirty(importer.assetPath);
                    importer.SaveAndReimport();
                }

                CreateInternalEditor();
                GUIUtility.ExitGUI();
            }
        }
    }

    private static void TryDisableInternalPostProcessorsUI(Editor internalEditor)
    {
        if (internalEditor == null) return;

        var t = internalEditor.GetType();
        while (t != null)
        {
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(bool)) continue;

                var n = f.Name;
                if (n.IndexOf("Post", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    n.IndexOf("Processor", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try { f.SetValue(internalEditor, false); }
                    catch { }
                }
            }
            t = t.BaseType;
        }
    }

    private enum FixKind
    {
        FPS,
        TPS_Clamp_UpperBody,
        TPS_Clamp_CreateMask,
        TPS_Clamp_FullBody,
        TPS_Loop_UpperBody,
        TPS_Loop_CreateMask,
        TPS_Loop_FullBody,
    }

    private void ApplyToSelected(FixKind kind, float scale = 1)
    {
        foreach (var t in targets)
        {
            var importer = t as ModelImporter;
            if (!importer) continue;

            var obj = AssetDatabase.LoadMainAssetAtPath(importer.assetPath);
            if (!obj) continue;

            Selection.activeObject = obj;

            switch (kind)
            {
                case FixKind.FPS:
                    FixFPSAnim(scale);
                    break;

                case FixKind.TPS_Clamp_UpperBody:
                    SkeletonFixer_FbxInspectorHooks.FixTPS(loop: false, mode: 0);
                    break;
                case FixKind.TPS_Clamp_CreateMask:
                    SkeletonFixer_FbxInspectorHooks.FixTPS(loop: false, mode: 1);
                    break;
                case FixKind.TPS_Clamp_FullBody:
                    SkeletonFixer_FbxInspectorHooks.FixTPS(loop: false, mode: 2);
                    break;

                case FixKind.TPS_Loop_UpperBody:
                    SkeletonFixer_FbxInspectorHooks.FixTPS(loop: true, mode: 0);
                    break;
                case FixKind.TPS_Loop_CreateMask:
                    SkeletonFixer_FbxInspectorHooks.FixTPS(loop: true, mode: 1);
                    break;
                case FixKind.TPS_Loop_FullBody:
                    SkeletonFixer_FbxInspectorHooks.FixTPS(loop: true, mode: 2);
                    break;
            }
        }
    }


    public static void FixFPSAnim(float scale = 1)
    {
        if (!IsNewActionTime()) return;

        foreach (UnityEngine.Object activeObject in Selection.objects)
        {
            if (activeObject == null)
            {
                Debug.Log("Please select a GameObject in the scene or a model asset in the project view.");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(activeObject);
            UnityEditor.ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as UnityEditor.ModelImporter;

            if (modelImporter != null)
            {
                modelImporter.globalScale = scale;
                FixAnimation(modelImporter, activeObject);
                ExtractAnimation(modelImporter, assetPath);
            }
            else
            {
                Debug.Log("Selected object is not a model asset.");
            }
        }
    }
    public static void FixAnimation(UnityEditor.ModelImporter singleImporter, UnityEngine.Object selectedObject, float error = .1f)
    {
        singleImporter.animationPositionError = error;
        singleImporter.animationRotationError = error;
        singleImporter.animationScaleError = error;
        singleImporter.resampleCurves = true;
        singleImporter.materialImportMode = ModelImporterMaterialImportMode.None;
        singleImporter.removeConstantScaleCurves = true;

#if UNITY_2022
        singleImporter.importBlendShapeDeformPercent = false;
#endif

        List<ModelImporterClipAnimation> validClips = new List<ModelImporterClipAnimation>();
        foreach (ModelImporterClipAnimation clip in singleImporter.defaultClipAnimations)
        {
            if (clip.lastFrame > 1)
            {
                Debug.Log(clip.name + " -> " + selectedObject.name + " (last frame: " + clip.lastFrame + ")");
                clip.name = selectedObject.name;
                clip.wrapMode = WrapMode.ClampForever;
                validClips.Add(clip);
            }
        }

        singleImporter.clipAnimations = validClips.ToArray();
        validClips.Clear();
        singleImporter.SaveAndReimport();
    }
    private static void ExtractAnimation(UnityEditor.ModelImporter modelImporter, string assetPath)
    {
        if (modelImporter == null)
        {
            Debug.LogError("Selected asset is not a model with a ModelImporter.");
            return;
        }

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (UnityEngine.Object asset in assets)
        {
            AnimationClip clip = asset as AnimationClip;
            if (clip != null && !clip.name.Contains("__preview__"))
            {
                if (AssetDatabase.GetAssetPath(clip) == assetPath)
                {
                    string folderPath = Path.GetDirectoryName(assetPath);
                    string newAssetPath = Path.Combine(folderPath, clip.name + ".anim");
                    newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

                    AnimationClip newClip = UnityEngine.Object.Instantiate(clip);
                    newClip.name = clip.name;
                    newClip.legacy = true;

                    AssetDatabase.CreateAsset(newClip, newAssetPath);
                    AssetImporter importer = AssetImporter.GetAtPath(newAssetPath);
                    importer.assetBundleName = "er2animations";
                    Debug.Log("Extracted animation clip: " + newAssetPath);
                }
                else
                {
                    Debug.Log("Clip '" + clip.name + "' is already extracted.");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }


    public enum TpsMaskMode
    {
        UpperBodyExistingMask,
        CreateMaskFromModel,
        DefaultFullBodyExistingMask
    }
    public static void FixTPSAnim(bool loop, TpsMaskMode mode)
    {
        if (!IsNewActionTime()) return;

        foreach (UnityEngine.Object activeObject in Selection.objects)
        {
            if (activeObject == null)
            {
                Debug.Log("Please select a GameObject in the scene or a model asset in the project view.");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(activeObject);
            UnityEditor.ModelImporter modelImporter = AssetImporter.GetAtPath(assetPath) as UnityEditor.ModelImporter;

            if (modelImporter == null)
            {
                Debug.Log("Selected object is not a model asset.");
                continue;
            }

            modelImporter.globalScale = 1f;

            // Keep your error defaults:
            // - Loop: 0.5
            // - Clamp: 0.5
            FixAnimation(modelImporter, activeObject, .5f);

            AvatarMask resolvedMask = ResolveTpsMaskForModel(activeObject, mode);
            FixAnimationTPS(modelImporter, activeObject, loop, resolvedMask);


            AddMarkerEventsToAllClips(assetPath);
        }
    }
    private const string MarkerRootPath = "Armature/Markers";
    private const float MarkerOnThreshold = 0.5f;

    private static void AddMarkerEventsToAllClips(string assetPath)
    {
        UnityEditor.ModelImporter importer = AssetImporter.GetAtPath(assetPath) as UnityEditor.ModelImporter;
        if (importer == null)
        {
            Debug.LogError("AddMarkerEventsToAllClips: not a ModelImporter: " + assetPath);
            return;
        }

        UnityEngine.Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        var runtimeClips = allAssets
            .OfType<AnimationClip>()
            .Where(c => !c.name.Contains("__preview__"))
            .ToDictionary(c => c.name, c => c);

        var importerClips = importer.clipAnimations;
        if (importerClips == null || importerClips.Length == 0)
        {
            Debug.LogWarning("AddMarkerEventsToAllClips: no importer clips on " + assetPath, importer);
            return;
        }

        for (int i = 0; i < importerClips.Length; i++)
        {
            var iclip = importerClips[i];
            if (!runtimeClips.TryGetValue(iclip.name, out var runtimeClip))
            {
                Debug.LogWarning($"[MarkerEvents] No runtime clip for importer clip '{iclip.name}'", importer);
                continue;
            }

            var markerEvents = BuildMarkerEventsFromClip(runtimeClip);

            // Overwrite only markers (as in your original comments; currently overwriting all events):
            iclip.events = markerEvents;

            importerClips[i] = iclip;
            Debug.Log($"[MarkerEvents] Importer clip '{iclip.name}' -> {iclip.events.Length} events", importer);
        }

        importer.clipAnimations = importerClips;
        importer.SaveAndReimport();
    }
    public static void FixAnimationTPS(UnityEditor.ModelImporter singleImporter, UnityEngine.Object selectedObject, bool loop, AvatarMask mask = null)
    {
        // If mask is null, your old behavior creates a temp full mask
        AvatarMask tempFullMask = null;
        if (mask == null) tempFullMask = BuildTempFullMaskFromModel(selectedObject);

        List<ModelImporterClipAnimation> validClips = new List<ModelImporterClipAnimation>();
        foreach (ModelImporterClipAnimation clip in singleImporter.defaultClipAnimations)
        {
            if (clip.lastFrame > 1)
            {
                Debug.Log(clip.name + " -> " + selectedObject.name);
                clip.name = selectedObject.name;
                clip.wrapMode = WrapMode.ClampForever;
                validClips.Add(clip);
            }

            clip.loop = loop;
            clip.loopTime = loop;

            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionXZ = true;
            clip.keepOriginalPositionY = true;

            clip.heightOffset = 0;

            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;

            if (mask != null)
            {
                clip.maskType = ClipAnimationMaskType.CopyFromOther;
                clip.maskSource = mask;
                clip.ConfigureClipFromMask(mask);
            }
            else if (tempFullMask != null)
            {
                clip.maskType = ClipAnimationMaskType.CreateFromThisModel;
                clip.ConfigureClipFromMask(tempFullMask);
            }
            else
            {
                clip.maskType = ClipAnimationMaskType.None;
            }

        }

        singleImporter.clipAnimations = validClips.ToArray();
        validClips.Clear();

        singleImporter.animationType = ModelImporterAnimationType.Human;
        singleImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        singleImporter.sourceAvatar = GetAvatar();
        singleImporter.globalScale = 1;

        singleImporter.SaveAndReimport();
    }
    private static AnimationEvent[] BuildMarkerEventsFromClip(AnimationClip clip)
    {
        var events = new List<AnimationEvent>();
        if (clip.length <= 0f) return events.ToArray();

        const float onThreshold = 0.7f;
        const float offThreshold = 0.3f;

        var bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var binding in bindings)
        {
            if (!binding.path.StartsWith(MarkerRootPath)) continue;
            if (binding.propertyName != "m_LocalScale.x") continue;

            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null || curve.keys == null || curve.keys.Length == 0) continue;

            string nodeName = binding.path.Substring(binding.path.LastIndexOf('/') + 1);
            const string separator = "___";
            int index = nodeName.IndexOf(separator, System.StringComparison.Ordinal);
            if (index < 0) continue;

            string nodeFunction = nodeName.Substring(0, index);
            string nodeParams = nodeName.Substring(index + separator.Length);

            int lastState = -1;

            foreach (var key in curve.keys.OrderBy(k => k.time))
            {
                float scale = key.value;

                int newState;
                if (scale >= onThreshold) newState = 1;
                else if (scale <= offThreshold) newState = 0;
                else continue;

                bool createEvent = false;
                float eventValue = newState;

                if (lastState == -1)
                {
                    if (newState == 1) createEvent = true;
                }
                else if (lastState != newState)
                {
                    createEvent = true;
                }

                if (createEvent)
                {
                    float normalizedTime = key.time / clip.length;
                    var ev = new AnimationEvent
                    {
                        time = normalizedTime,
                        functionName = nodeFunction,
                        stringParameter = nodeParams,
                        floatParameter = eventValue
                    };
                    events.Add(ev);
                }

                lastState = newState;
            }
        }

        return events.ToArray();
    }

    [MenuItem("Assets/ER2 TOOLS/DEBUG/Print Clip Bindings", false, 5000)]
    private static void DebugPrintClipBindings()
    {
        foreach (UnityEngine.Object obj in Selection.objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            foreach (var clip in assets.OfType<AnimationClip>())
            {
                if (clip.name.Contains("__preview__")) continue;
                Debug.Log($"--- Clip: {clip.name} ---", clip);

                var bindings = AnimationUtility.GetCurveBindings(clip);
                foreach (var b in bindings)
                {
                    Debug.Log($"Path: {b.path} | Property: {b.propertyName}", clip);
                }
            }
        }
    }

    private static AvatarMask ResolveTpsMaskForModel(UnityEngine.Object modelAsset, TpsMaskMode mode)
    {
        switch (mode)
        {
            case TpsMaskMode.UpperBodyExistingMask:
                {
                    // same as your original behavior: use the project asset directly
                    return GetAvatarMask("UpperBodyMask_wBones");
                }

            case TpsMaskMode.DefaultFullBodyExistingMask:
                {
                    // new option: same behavior as upper body, just different mask name
                    return GetAvatarMask("FullBodyMask");
                }

            case TpsMaskMode.CreateMaskFromModel:
            default:
                // keep same as original: null here means FixAnimationTPS will CreateFromThisModel
                return null;
        }
    }
    private static Avatar GetAvatar()
    {
        string[] guids = AssetDatabase.FindAssets("ER2_AllUniforms_UnityExport t:Avatar");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Avatar avatar = (Avatar)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Avatar));
            return avatar;
        }
        return null;
    }
    private static AvatarMask GetAvatarMask(string maskName)
    {
        string[] guids = AssetDatabase.FindAssets(maskName + " t:AvatarMask");
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AvatarMask avatar = (AvatarMask)AssetDatabase.LoadAssetAtPath(assetPath, typeof(AvatarMask));
            if (avatar && avatar.name == maskName) return avatar;
        }
        return null;
    }

    private static AvatarMask BuildTempFullMaskFromModel(UnityEngine.Object modelAsset)
    {
        string assetPath = AssetDatabase.GetAssetPath(modelAsset);
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (!root)
        {
            Debug.LogError("Cannot load model root for temp mask: " + assetPath, modelAsset);
            return null;
        }

        var mask = new AvatarMask();
        mask.AddTransformPath(root.transform, true);
        return mask;
    }
}

public static class SkeletonFixer_FbxInspectorHooks
{
    public static void FixTPS(bool loop, int mode)
    {
        ModelImporterSkeletonFixerEditor.FixTPSAnim(loop, (ModelImporterSkeletonFixerEditor.TpsMaskMode)mode);
    }
}

public partial class MaterialWizardUI
{
    public static void DrawUI(UnityEngine.Object[] targets)
    {
        Draw(targets);
    }
    static partial void Draw(UnityEngine.Object[] targets);

    public static void Initialize()
    {
        Init();
    }
    static partial void Init();
}