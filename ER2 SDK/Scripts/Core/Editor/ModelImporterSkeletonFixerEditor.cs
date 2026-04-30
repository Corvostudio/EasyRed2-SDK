using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

[CustomEditor(typeof(ModelImporter))]
[CanEditMultipleObjects]
public class ModelImporterSkeletonFixerEditor : Editor
{
    // ─────────────────────────────────────────────────────────────
    // Constants
    // ─────────────────────────────────────────────────────────────
    private const string AnimationAssetBundleName = "er2animations";
    private const string DefaultAvatarSearch = "ER2_AllUniforms_UnityExport t:Avatar";
    private const string UpperBodyMaskName = "UpperBodyMask_wBones";
    private const string FullBodyMaskName = "FullBodyMask";

    private const string MarkerRootPath = "Armature/Markers";
    private const string MarkerNodeSeparator = "___";
    private const float MarkerOnThreshold = 0.7f;
    private const float MarkerOffThreshold = 0.3f;

    private const float ActionCooldownSeconds = 0.5f;
    private const float FpsAnimError = 0.1f;
    private const float TpsAnimError = 0.5f;

    // ─────────────────────────────────────────────────────────────
    // Editor state
    // ─────────────────────────────────────────────────────────────
    private Editor _internalEditor;
    private bool _fpsWorkflow = true;
    private bool _tpsWorkflowLoop = false;

    // Global click cooldown shared by FPS and TPS buttons / public statics.
    private static double s_nextActionTime = 0.0;

    // ─────────────────────────────────────────────────────────────
    // Editor lifecycle
    // ─────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        MaterialWizardUI.Initialize();
        CreateInternalEditor();
    }

    private void OnDisable()
    {
        if (_internalEditor != null)
            DestroyImmediate(_internalEditor);
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

        // 0=Model, 1=Rig, 2=Animation, 3=Materials. -1 = unknown -> show everything.
        int tab = TryGetInternalTabIndex(_internalEditor);
        if (tab == 2 || tab == -1) DrawAnimationFixUI();
        if (tab == 3 || tab == -1) MaterialWizardUI.DrawUI(targets);
    }

    // ─────────────────────────────────────────────────────────────
    // Internal editor management
    // ─────────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────
    // UI: Animation tab
    // ─────────────────────────────────────────────────────────────
    private void DrawAnimationFixUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("EASY RED 2 – Animation Extractor", EditorStyles.boldLabel);

        if (SkeletonFixer_FbxInspectorHooks.AllowTps)
            _fpsWorkflow = EditorGUILayout.Toggle("FPS", _fpsWorkflow);
        else
            _fpsWorkflow = true;

        using (new EditorGUILayout.VerticalScope("box"))
        {
            if (_fpsWorkflow) DrawFpsButtons();
            else DrawTpsButtons();
        }
    }

    private void DrawFpsButtons()
    {
        EditorGUILayout.LabelField("Extract FPS Animation", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Maya Scale: 100x")) ApplyToSelected(FixKind.FPS, 100f);
            if (GUILayout.Button("Blender Scale: .01x")) ApplyToSelected(FixKind.FPS, 0.01f);
            if (GUILayout.Button("Scale: 1x")) ApplyToSelected(FixKind.FPS, 1f);
        }
    }

    private void DrawTpsButtons()
    {
        EditorGUILayout.LabelField("Set as TPS Animation", EditorStyles.boldLabel);
        _tpsWorkflowLoop = EditorGUILayout.Toggle("Loop", _tpsWorkflowLoop);

        // Branches were swapped in the original code: the "Loop" toggle was
        // dispatching the Clamp variants and vice-versa. Now they match.
        using (new EditorGUILayout.HorizontalScope())
        {
            if (_tpsWorkflowLoop)
            {
                if (GUILayout.Button("Upper body")) ApplyToSelected(FixKind.TPS_Loop_UpperBody);
                if (GUILayout.Button("Create mask")) ApplyToSelected(FixKind.TPS_Loop_CreateMask);
                if (GUILayout.Button("Default Full Body")) ApplyToSelected(FixKind.TPS_Loop_FullBody);
            }
            else
            {
                if (GUILayout.Button("Upper body")) ApplyToSelected(FixKind.TPS_Clamp_UpperBody);
                if (GUILayout.Button("Create mask")) ApplyToSelected(FixKind.TPS_Clamp_CreateMask);
                if (GUILayout.Button("Default Full Body")) ApplyToSelected(FixKind.TPS_Clamp_FullBody);
            }
        }
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

    // ─────────────────────────────────────────────────────────────
    // Action dispatch
    // ─────────────────────────────────────────────────────────────
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

    public enum TpsMaskMode
    {
        UpperBodyExistingMask,
        CreateMaskFromModel,
        DefaultFullBodyExistingMask
    }

    /// <summary>
    /// Dispatches a fix to every selected target. One cooldown per click,
    /// applied to the whole batch (multi-select friendly).
    /// </summary>
    private void ApplyToSelected(FixKind kind, float scale = 1f)
    {
        if (!ConsumeCooldown()) return;

        foreach (var t in targets)
        {
            var importer = t as ModelImporter;
            if (!importer) continue;

            var asset = AssetDatabase.LoadMainAssetAtPath(importer.assetPath);
            if (!asset) continue;

            switch (kind)
            {
                case FixKind.FPS:
                    FixFPSAnimSingle(importer, asset, scale);
                    break;

                case FixKind.TPS_Clamp_UpperBody:
                    FixTPSAnimSingle(importer, asset, loop: false, mode: TpsMaskMode.UpperBodyExistingMask);
                    break;
                case FixKind.TPS_Clamp_CreateMask:
                    FixTPSAnimSingle(importer, asset, loop: false, mode: TpsMaskMode.CreateMaskFromModel);
                    break;
                case FixKind.TPS_Clamp_FullBody:
                    FixTPSAnimSingle(importer, asset, loop: false, mode: TpsMaskMode.DefaultFullBodyExistingMask);
                    break;

                case FixKind.TPS_Loop_UpperBody:
                    FixTPSAnimSingle(importer, asset, loop: true, mode: TpsMaskMode.UpperBodyExistingMask);
                    break;
                case FixKind.TPS_Loop_CreateMask:
                    FixTPSAnimSingle(importer, asset, loop: true, mode: TpsMaskMode.CreateMaskFromModel);
                    break;
                case FixKind.TPS_Loop_FullBody:
                    FixTPSAnimSingle(importer, asset, loop: true, mode: TpsMaskMode.DefaultFullBodyExistingMask);
                    break;
            }
        }
    }

    private static bool ConsumeCooldown()
    {
        if (EditorApplication.timeSinceStartup > s_nextActionTime)
        {
            s_nextActionTime = EditorApplication.timeSinceStartup + ActionCooldownSeconds;
            return true;
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────────
    // FPS workflow
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Public entry point for external callers (menu items, etc.). Iterates
    /// Selection.objects. Internally enforces the click cooldown.
    /// </summary>
    public static void FixFPSAnim(float scale = 1f)
    {
        if (!ConsumeCooldown()) return;

        foreach (var obj in Selection.objects)
        {
            if (obj == null) continue;
            string assetPath = AssetDatabase.GetAssetPath(obj);
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.Log("Selected object is not a model asset: " + obj.name);
                continue;
            }
            FixFPSAnimSingle(importer, obj, scale);
        }
    }

    private static void FixFPSAnimSingle(ModelImporter importer, UnityEngine.Object asset, float scale)
    {
        importer.globalScale = scale;
        FixAnimation(importer, asset, FpsAnimError);
        ExtractAnimation(importer, importer.assetPath);
    }

    /// <summary>
    /// Common importer/clip setup. Public for backward compatibility.
    /// </summary>
    public static void FixAnimation(ModelImporter singleImporter, UnityEngine.Object selectedObject, float error = 0.1f)
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

        var validClips = new List<ModelImporterClipAnimation>();
        foreach (var clip in singleImporter.defaultClipAnimations)
        {
            if (clip.lastFrame <= 1) continue;

            Debug.Log($"{clip.name} -> {selectedObject.name} (last frame: {clip.lastFrame})");
            clip.name = selectedObject.name;
            clip.wrapMode = WrapMode.ClampForever;
            validClips.Add(clip);
        }

        singleImporter.clipAnimations = validClips.ToArray();
        singleImporter.SaveAndReimport();
    }

    private static void ExtractAnimation(ModelImporter modelImporter, string assetPath)
    {
        if (modelImporter == null)
        {
            Debug.LogError("Selected asset is not a model with a ModelImporter.");
            return;
        }

        var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (var asset in assets)
        {
            var clip = asset as AnimationClip;
            if (clip == null) continue;
            if (clip.name.Contains("__preview__")) continue;

            if (AssetDatabase.GetAssetPath(clip) != assetPath)
            {
                Debug.Log("Clip '" + clip.name + "' is already extracted.");
                continue;
            }

            string folderPath = Path.GetDirectoryName(assetPath);
            string newAssetPath = Path.Combine(folderPath, clip.name + ".anim");
            newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

            var newClip = UnityEngine.Object.Instantiate(clip);
            newClip.name = clip.name;
            newClip.legacy = true;

            AssetDatabase.CreateAsset(newClip, newAssetPath);
            var newImporter = AssetImporter.GetAtPath(newAssetPath);
            if (newImporter != null) newImporter.assetBundleName = AnimationAssetBundleName;

            Debug.Log("Extracted animation clip: " + newAssetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ─────────────────────────────────────────────────────────────
    // TPS workflow
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Public entry point for external callers. Iterates Selection.objects.
    /// Internally enforces the click cooldown.
    /// </summary>
    public static void FixTPSAnim(bool loop, TpsMaskMode mode)
    {
        if (!ConsumeCooldown()) return;

        foreach (var obj in Selection.objects)
        {
            if (obj == null) continue;
            string assetPath = AssetDatabase.GetAssetPath(obj);
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
            {
                Debug.Log("Selected object is not a model asset: " + obj.name);
                continue;
            }
            FixTPSAnimSingle(importer, obj, loop, mode);
        }
    }

    private static void FixTPSAnimSingle(ModelImporter importer, UnityEngine.Object asset, bool loop, TpsMaskMode mode)
    {
        importer.globalScale = 1f;

        // Importer-level settings + naming pass.
        FixAnimation(importer, asset, TpsAnimError);

        // Resolve the avatar mask (may be null -> FixAnimationTPS will build a temp full mask).
        AvatarMask resolvedMask = ResolveTpsMaskForModel(asset, mode);
        FixAnimationTPS(importer, asset, loop, resolvedMask);

        AddMarkerEventsToAllClips(importer.assetPath);
    }

    /// <summary>
    /// Configures clip animations for a humanoid TPS pipeline. Public for backward compatibility.
    /// </summary>
    public static void FixAnimationTPS(ModelImporter singleImporter, UnityEngine.Object selectedObject, bool loop, AvatarMask mask = null)
    {
        // If no mask was passed, build a temporary mask from the model and remember to dispose it.
        AvatarMask tempFullMask = (mask == null) ? BuildTempFullMaskFromModel(selectedObject) : null;

        try
        {
            var validClips = new List<ModelImporterClipAnimation>();
            foreach (var clip in singleImporter.defaultClipAnimations)
            {
                if (clip.lastFrame <= 1) continue;

                Debug.Log(clip.name + " -> " + selectedObject.name);

                clip.name = selectedObject.name;
                clip.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;

                clip.loop = loop;
                clip.loopTime = loop;

                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionXZ = true;
                clip.keepOriginalPositionY = true;
                clip.heightOffset = 0f;
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

                validClips.Add(clip);
            }

            singleImporter.clipAnimations = validClips.ToArray();
            singleImporter.animationType = ModelImporterAnimationType.Human;
            singleImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            singleImporter.sourceAvatar = GetAvatar();
            singleImporter.globalScale = 1f;

            singleImporter.SaveAndReimport();
        }
        finally
        {
            // Avoid leaking the temporary AvatarMask UnityEngine.Object.
            if (tempFullMask != null)
                UnityEngine.Object.DestroyImmediate(tempFullMask);
        }
    }

    private static AvatarMask ResolveTpsMaskForModel(UnityEngine.Object modelAsset, TpsMaskMode mode)
    {
        switch (mode)
        {
            case TpsMaskMode.UpperBodyExistingMask: return GetAvatarMask(UpperBodyMaskName);
            case TpsMaskMode.DefaultFullBodyExistingMask: return GetAvatarMask(FullBodyMaskName);
            case TpsMaskMode.CreateMaskFromModel: return null; // FixAnimationTPS will build a temp mask
            default: return null;
        }
    }

    private static AvatarMask BuildTempFullMaskFromModel(UnityEngine.Object modelAsset)
    {
        string assetPath = AssetDatabase.GetAssetPath(modelAsset);
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (!root)
        {
            Debug.LogError("Cannot load model root for temp mask: " + assetPath, modelAsset);
            return null;
        }

        var mask = new AvatarMask();
        mask.AddTransformPath(root.transform, true);
        return mask;
    }

    private static Avatar GetAvatar()
    {
        foreach (var guid in AssetDatabase.FindAssets(DefaultAvatarSearch))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(p);
            if (avatar != null) return avatar;
        }
        return null;
    }

    private static AvatarMask GetAvatarMask(string maskName)
    {
        foreach (var guid in AssetDatabase.FindAssets(maskName + " t:AvatarMask"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<AvatarMask>(p);
            if (m != null && m.name == maskName) return m;
        }
        return null;
    }

    // ─────────────────────────────────────────────────────────────
    // Marker events
    // ─────────────────────────────────────────────────────────────
    private static void AddMarkerEventsToAllClips(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError("AddMarkerEventsToAllClips: not a ModelImporter: " + assetPath);
            return;
        }

        // GroupBy/First so we don't throw if Unity exposes duplicate clip names.
        var runtimeClips = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .Where(c => !c.name.Contains("__preview__"))
            .GroupBy(c => c.name)
            .ToDictionary(g => g.Key, g => g.First());

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

            iclip.events = BuildMarkerEventsFromClip(runtimeClip);
            importerClips[i] = iclip;
            Debug.Log($"[MarkerEvents] Importer clip '{iclip.name}' -> {iclip.events.Length} events", importer);
        }

        importer.clipAnimations = importerClips;
        importer.SaveAndReimport();
    }

    private static AnimationEvent[] BuildMarkerEventsFromClip(AnimationClip clip)
    {
        var events = new List<AnimationEvent>();
        if (clip.length <= 0f) return events.ToArray();

        var bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var binding in bindings)
        {
            if (!binding.path.StartsWith(MarkerRootPath)) continue;
            if (binding.propertyName != "m_LocalScale.x") continue;

            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null || curve.keys == null || curve.keys.Length == 0) continue;

            string nodeName = binding.path.Substring(binding.path.LastIndexOf('/') + 1);
            int sepIndex = nodeName.IndexOf(MarkerNodeSeparator, StringComparison.Ordinal);
            if (sepIndex < 0) continue;

            string nodeFunction = nodeName.Substring(0, sepIndex);
            string nodeParams = nodeName.Substring(sepIndex + MarkerNodeSeparator.Length);

            int lastState = -1;
            foreach (var key in curve.keys.OrderBy(k => k.time))
            {
                int newState;
                if (key.value >= MarkerOnThreshold) newState = 1;
                else if (key.value <= MarkerOffThreshold) newState = 0;
                else continue;

                bool createEvent =
                    (lastState == -1 && newState == 1) ||
                    (lastState != -1 && lastState != newState);

                if (createEvent)
                {
                    events.Add(new AnimationEvent
                    {
                        time = key.time / clip.length, // normalized
                        functionName = nodeFunction,
                        stringParameter = nodeParams,
                        floatParameter = newState
                    });
                }

                lastState = newState;
            }
        }

        return events.ToArray();
    }

    // ─────────────────────────────────────────────────────────────
    // Reflection helpers (Unity internals)
    // ─────────────────────────────────────────────────────────────
    private static int TryGetInternalTabIndex(Editor internalEditor)
    {
        if (internalEditor == null) return -1;

        // Tightened heuristic: match well-known field name fragments only.
        var t = internalEditor.GetType();
        while (t != null)
        {
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var f in fields)
            {
                if (f.FieldType != typeof(int)) continue;
                string n = f.Name;

                bool likelyTabField =
                    n.IndexOf("ActiveTab", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("TabIndex", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("ActiveEditor", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!likelyTabField) continue;

                try
                {
                    int v = (int)f.GetValue(internalEditor);
                    if (v >= 0 && v <= 6) return v;
                }
                catch { /* ignore */ }
            }
            t = t.BaseType;
        }
        return -1;
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
                string n = f.Name;
                if (n.IndexOf("Post", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    n.IndexOf("Processor", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try { f.SetValue(internalEditor, false); }
                    catch { /* ignore */ }
                }
            }
            t = t.BaseType;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Debug menu
    // ─────────────────────────────────────────────────────────────
    [MenuItem("Assets/ER2 TOOLS/DEBUG/Print Clip Bindings", false, 5000)]
    private static void DebugPrintClipBindings()
    {
        foreach (var obj in Selection.objects)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            foreach (var clip in assets.OfType<AnimationClip>())
            {
                if (clip.name.Contains("__preview__")) continue;
                Debug.Log($"--- Clip: {clip.name} ---", clip);

                var bindings = AnimationUtility.GetCurveBindings(clip);
                foreach (var b in bindings)
                    Debug.Log($"Path: {b.path} | Property: {b.propertyName}", clip);
            }
        }
    }
}

// ─────────────────────────────────────────────────────────────────
// Public hook used by external callers (signature preserved).
// ─────────────────────────────────────────────────────────────────
public static partial class SkeletonFixer_FbxInspectorHooks
{
    public static void FixTPS(bool loop, int mode)
    {
        ModelImporterSkeletonFixerEditor.FixTPSAnim(
            loop,
            (ModelImporterSkeletonFixerEditor.TpsMaskMode)mode);
    }

    public static bool AllowTps
    {
        get
        {
            bool allow = false;
            AllowTPSSetup(ref allow);
            return allow;
        }
    }
    static partial void AllowTPSSetup(ref bool allow);
}

// ─────────────────────────────────────────────────────────────────
// Material wizard partial — Init/Draw bodies live in another partial file.
// ─────────────────────────────────────────────────────────────────
public partial class MaterialWizardUI
{
    public static void DrawUI(UnityEngine.Object[] targets) => Draw(targets);
    public static void Initialize() => Init();

    static partial void Draw(UnityEngine.Object[] targets);
    static partial void Init();
}