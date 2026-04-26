using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class TPSRigPreviewManager : MonoBehaviour
{
    public SkinnedMeshRenderer uniform;
    public SkinnedMeshRenderer uniform_lod;
    public SkinnedMeshRenderer vest;
    public SkinnedMeshRenderer vest_lod;
    public SkinnedMeshRenderer head;
    public SkinnedMeshRenderer head_lod;
    public SkinnedMeshRenderer hands;
    public SkinnedMeshRenderer hands_lod;
    public SkinnedMeshRenderer uniform_fps;
    public SkinnedMeshRenderer hands_fps;
    public Transform headgear_pos;


#if UNITY_EDITOR
    public static TPSRigPreviewManager GetTpsRigtester()
    {
        TPSRigPreviewManager tester = GameObject.FindObjectOfType<TPSRigPreviewManager>();
        if (tester == null)
        {
            GameObject animRoot = (GameObject)PrefabUtility.InstantiatePrefab((GameObject)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/Rigs/tps rig tester.prefab", typeof(GameObject)));
            animRoot.transform.localPosition = animRoot.transform.localEulerAngles = Vector3.zero;
            animRoot.transform.localScale = Vector3.one;
            tester = animRoot.GetComponent<TPSRigPreviewManager>();
        }
        return tester;
    }

    private static double next_action_time = 0;
    public static bool IsNewActionTime()
    {
        if (EditorApplication.timeSinceStartup > next_action_time)
        {
            next_action_time = EditorApplication.timeSinceStartup + .5f;
            return true;
        }
        return false;
    }

    [MenuItem("Assets/ER2 TOOLS/Tools/RIG Testing/Test rigged mesh", false, 2004)]
    public static void SetUpAnimationTesting()
    {
        if (!IsNewActionTime())
            return;

        foreach (Object obj in Selection.objects)
        {
            if (!(obj is Mesh))
            {
                Debug.LogError("Selected Model '" + obj + "' is not a skinned mesh.", obj);
                continue;
            }

            if (!GetSelectedSkinnedMeshRenderer((Mesh)obj, out SkinnedMeshRenderer originalMeshRenderer, out int bonecount))
                return;

            TPSRigPreviewManager tester = GetTpsRigtester();

            switch (bonecount)
            {
                case 2:
                    if (TestBoneOrder(originalMeshRenderer, tester.head, "Head"))
                    {
                        tester.head.sharedMesh = originalMeshRenderer.sharedMesh;
                        tester.head.sharedMaterials = originalMeshRenderer.sharedMaterials;
                    }
                    break;
                case 3:
                    if (TestBoneOrder(originalMeshRenderer, tester.vest, "Vest"))
                    {
                        tester.vest.sharedMesh = originalMeshRenderer.sharedMesh;
                        tester.vest.sharedMaterials = originalMeshRenderer.sharedMaterials;
                    }
                    break;
                case 4:
                    if (TestBoneOrder(originalMeshRenderer, tester.hands, "Hands"))
                    {
                        tester.hands.sharedMesh = originalMeshRenderer.sharedMesh;
                        tester.hands.sharedMaterials = originalMeshRenderer.sharedMaterials;
                    }
                    break;
                case 22:
                    if (TestBoneOrder(originalMeshRenderer, tester.uniform, "Uniform"))
                    {
                        tester.uniform.sharedMesh = originalMeshRenderer.sharedMesh;
                        tester.uniform.sharedMaterials = originalMeshRenderer.sharedMaterials;
                    }
                    break;
                case 51:
                    if (TestBoneOrder(originalMeshRenderer, tester.uniform_fps, "FPS Uniform"))
                    {
                        tester.uniform_fps.sharedMesh = originalMeshRenderer.sharedMesh;
                        tester.uniform_fps.sharedMaterials = originalMeshRenderer.sharedMaterials;
                    }
                    break;
                default:
                    Debug.LogError("Selected skinned mesh '" + originalMeshRenderer.sharedMesh + "' has wrong bone amount (" + bonecount + ").", originalMeshRenderer.sharedMesh);
                    break;
            }
        }
    }

    public static bool TestBoneOrder(SkinnedMeshRenderer originalSkinnedMeshRenderer, SkinnedMeshRenderer targetSkinnedMeshRenderer, string type)
    {
        if (originalSkinnedMeshRenderer.bones.Length != targetSkinnedMeshRenderer.bones.Length)
        {
            Debug.LogError("Selected skinned mesh '" + originalSkinnedMeshRenderer.sharedMesh + "' is not of type '" + type + "' or is not skinned to the correct bones.", originalSkinnedMeshRenderer.sharedMesh);
            return false;
        }

        for (int i = 0; i < originalSkinnedMeshRenderer.bones.Length; i++)
        {
            if (originalSkinnedMeshRenderer.bones[i].name != targetSkinnedMeshRenderer.bones[i].name)
            {
                Debug.LogError("Selected skinned mesh '" + originalSkinnedMeshRenderer.sharedMesh + "' is not of type '" + type + "' or has wrong bone order.", originalSkinnedMeshRenderer.sharedMesh);
                return false;
            }
        }

        Debug.Log("Uniform '" + originalSkinnedMeshRenderer.sharedMesh + "'  imported correctly.", originalSkinnedMeshRenderer.sharedMesh);
        return true;
    }

    public static bool GetSelectedSkinnedMeshRenderer(Mesh selectedMesh, out SkinnedMeshRenderer skinnedMeshRenderer, out int bonesCount)
    {
        bonesCount = 0;
        skinnedMeshRenderer = null;
        GameObject model = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(selectedMesh)) as GameObject;
        foreach (SkinnedMeshRenderer smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            if (smr.sharedMesh == (Mesh)selectedMesh)
            {
                skinnedMeshRenderer = smr;
                break;
            }
        }
        if (skinnedMeshRenderer == null)
        {
            Debug.LogError("Selected Model '" + selectedMesh + "' is not a skinned mesh.", selectedMesh);
            return false;
        }

        bonesCount = skinnedMeshRenderer.bones.Length;
        return true;
    }
#endif
}


#if UNITY_EDITOR
/// <summary>
/// Editor-only static manager that owns links between ItemClothing/ItemHelmet
/// and the single TPSRigPreviewManager in the scene. Periodically resyncs all
/// linked items so that material/mesh changes show up live.
/// </summary>
public static class TPSRigTesterManager
{
    private const string TESTER_PREFAB_PATH = "Assets/ER2 SDK/Rigs/tps rig tester.prefab";
    private const double SYNC_INTERVAL = 0.5;

    private static TPSRigPreviewManager s_cachedTester;

    private static int s_uniformID;
    private static SleeveMode s_uniformSleeveMode = SleeveMode.unrolled;
    private static int s_vestID;
    private static int s_handsID;
    private static int s_headGearID;
    private static int s_helmetID;

    private static GameObject s_spawnedHelmet;
    private static int s_spawnedHelmetSourceHash;

    private static double s_lastSync;
    private static bool s_registered;

    [InitializeOnLoadMethod]
    private static void Register()
    {
        if (s_registered) return;
        s_registered = true;
        EditorApplication.update += OnUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            DisableTester();
    }

    // ===== Tester lifecycle =====

    public static TPSRigPreviewManager FindTester()
    {
        if (s_cachedTester != null && !EditorUtility.IsPersistent(s_cachedTester) && s_cachedTester.gameObject.scene.IsValid())
            return s_cachedTester;

        s_cachedTester = null;
        TPSRigPreviewManager first = null;
        var all = Resources.FindObjectsOfTypeAll<TPSRigPreviewManager>();
        foreach (var t in all)
        {
            if (t == null) continue;
            if (EditorUtility.IsPersistent(t)) continue;
            if (!t.gameObject.scene.IsValid()) continue;

            if (first == null)
                first = t;
            else
                Object.DestroyImmediate(t.gameObject);
        }
        s_cachedTester = first;
        return first;
    }

    public static TPSRigPreviewManager GetOrSpawnTester()
    {
        var existing = FindTester();
        if (existing != null)
        {
            EnforceDontSaveFlags(existing.gameObject);
            return existing;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TESTER_PREFAB_PATH);
        if (prefab == null)
        {
            Debug.LogError($"[TPS Tester] Prefab not found at '{TESTER_PREFAB_PATH}'.");
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (go == null) return null;

        go.transform.localPosition = Vector3.zero;
        go.transform.localEulerAngles = Vector3.zero;
        go.transform.localScale = Vector3.one;

        EnforceDontSaveFlags(go);
        s_cachedTester = go.GetComponent<TPSRigPreviewManager>();
        return s_cachedTester;
    }

    private static void EnforceDontSaveFlags(GameObject root)
    {
        var flags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            t.gameObject.hideFlags = flags;
    }

    public static void DisableTester()
    {
        DespawnHelmet();
        var tester = FindTester();
        if (tester != null)
            Object.DestroyImmediate(tester.gameObject);
        s_cachedTester = null;
        s_uniformID = s_vestID = s_handsID = s_headGearID = s_helmetID = 0;
        RepaintRelevantInspectors();
    }

    // ===== Linking API =====

    public static void LinkUniform(ItemClothing c, SleeveMode sleeveMode)
    {
        if (c == null || GetOrSpawnTester() == null) return;
        s_uniformID = c.GetInstanceID();
        s_uniformSleeveMode = sleeveMode;
        s_lastSync = 0;
        SyncAll();
        SceneView.RepaintAll();
        RepaintRelevantInspectors();
    }

    public static void LinkGear(ItemClothing c)
    {
        if (c == null || c.mesh == null || GetOrSpawnTester() == null) return;
        int boneCount = c.mesh.bindposes.Length;
        switch (boneCount)
        {
            case 2: s_headGearID = c.GetInstanceID(); break;
            case 3: s_vestID = c.GetInstanceID(); break;
            case 4: s_handsID = c.GetInstanceID(); break;
            default:
                Debug.LogWarning($"[TPS Tester] Gear mesh '{c.mesh.name}' has {boneCount} bones, no matching slot.");
                return;
        }
        s_lastSync = 0;
        SyncAll();
        SceneView.RepaintAll();
        RepaintRelevantInspectors();
    }

    public static void LinkHelmet(ItemHelmet h)
    {
        if (h == null || GetOrSpawnTester() == null) return;
        s_helmetID = h.GetInstanceID();
        s_lastSync = 0;
        SyncAll();
        SceneView.RepaintAll();
        RepaintRelevantInspectors();
    }

    public static bool IsUniformLinked(ItemClothing c)
        => c != null && s_uniformID == c.GetInstanceID();

    public static bool IsGearLinked(ItemClothing c)
    {
        if (c == null) return false;
        int id = c.GetInstanceID();
        return s_vestID == id || s_handsID == id || s_headGearID == id;
    }

    public static bool IsHelmetLinked(ItemHelmet h)
        => h != null && s_helmetID == h.GetInstanceID();

    public static SleeveMode GetUniformSleeveMode() => s_uniformSleeveMode;

    public static string GetGearSlotName(ItemClothing c)
    {
        if (c == null) return "";
        int id = c.GetInstanceID();
        if (s_vestID == id) return "VEST";
        if (s_handsID == id) return "HANDS";
        if (s_headGearID == id) return "HEAD";
        return "";
    }

    public static void UnlinkUniform()
    {
        s_uniformID = 0;
        ClearUniformOnTester();
        RepaintRelevantInspectors();
    }

    public static void UnlinkGear(ItemClothing c)
    {
        if (c == null) return;
        int id = c.GetInstanceID();
        var tester = FindTester();
        if (s_vestID == id) { s_vestID = 0; if (tester != null) ClearRendererPair(tester.vest, tester.vest_lod); }
        if (s_handsID == id) { s_handsID = 0; if (tester != null) ClearRendererPair(tester.hands, tester.hands_lod); }
        if (s_headGearID == id) { s_headGearID = 0; if (tester != null) ClearRendererPair(tester.head, tester.head_lod); }
        RepaintRelevantInspectors();
    }

    public static void UnlinkHelmet()
    {
        s_helmetID = 0;
        DespawnHelmet();
        RepaintRelevantInspectors();
    }

    // ===== Update loop =====

    private static void OnUpdate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.timeSinceStartup - s_lastSync < SYNC_INTERVAL) return;
        s_lastSync = EditorApplication.timeSinceStartup;
        SyncAll();
    }

    private static void SyncAll()
    {
        var tester = FindTester();
        if (tester == null)
        {
            s_uniformID = s_vestID = s_handsID = s_headGearID = s_helmetID = 0;
            DespawnHelmet();
            return;
        }
        SyncUniform(tester);
        SyncGear(tester, ref s_vestID, tester.vest, tester.vest_lod);
        SyncGear(tester, ref s_handsID, tester.hands, tester.hands_lod);
        SyncGear(tester, ref s_headGearID, tester.head, tester.head_lod);
        SyncHelmet(tester);
    }

    private static ItemClothing GetClothing(int id)
        => id == 0 ? null : EditorUtility.InstanceIDToObject(id) as ItemClothing;
    private static ItemHelmet GetHelmet(int id)
        => id == 0 ? null : EditorUtility.InstanceIDToObject(id) as ItemHelmet;

    private static void SyncUniform(TPSRigPreviewManager tester)
    {
        var c = GetClothing(s_uniformID);
        if (c == null) { s_uniformID = 0; return; }

        bool useShort = s_uniformSleeveMode == SleeveMode.rolled && c.mesh_short_sleeve != null;
        Mesh tpsMesh = useShort ? c.mesh_short_sleeve : c.mesh;
        Mesh tpsMeshLod = useShort ? c.mesh_short_sleeve_lod : c.mesh_lod;

        if (tester.uniform != null)
        {
            tester.uniform.sharedMesh = tpsMesh;
            tester.uniform.sharedMaterials = c.GetTpsMaterials(s_uniformSleeveMode);
        }
        if (tester.uniform_lod != null)
        {
            tester.uniform_lod.sharedMesh = tpsMeshLod != null ? tpsMeshLod : tpsMesh;
            tester.uniform_lod.sharedMaterials = c.GetTpsMaterialsLOD(s_uniformSleeveMode);
        }
        if (tester.uniform_fps != null && c.fps_mesh != null)
        {
            Mesh fpsMesh = (useShort && c.fps_mesh_short_sleeve != null) ? c.fps_mesh_short_sleeve : c.fps_mesh;
            tester.uniform_fps.sharedMesh = fpsMesh;
            tester.uniform_fps.sharedMaterials = c.GetFpsMaterials(s_uniformSleeveMode);
        }

        var handsMode = useShort ? c.handsModeWithShortSleeveUniform : c.forceHideHands;
        bool hideHands = handsMode == UniformSleeveCoverType.coversSleeveAndHand;
        SetActiveSafe(tester.hands, !hideHands);
        SetActiveSafe(tester.hands_lod, !hideHands);
        SetActiveSafe(tester.hands_fps, !hideHands);

        bool hideHead = c.forceHideHead != UniformHeadCoverType.dontCoverHead;
        SetActiveSafe(tester.head, !hideHead);
        SetActiveSafe(tester.head_lod, !hideHead);
    }

    private static void SyncGear(TPSRigPreviewManager tester, ref int slotID, SkinnedMeshRenderer hp, SkinnedMeshRenderer lod)
    {
        if (slotID == 0) return;
        var c = GetClothing(slotID);
        if (c == null) { slotID = 0; return; }

        if (hp != null)
        {
            hp.sharedMesh = c.mesh;
            hp.sharedMaterials = c.GetTpsMaterials();
            SetActiveSafe(hp, true);
        }
        if (lod != null)
        {
            lod.sharedMesh = c.mesh_lod != null ? c.mesh_lod : c.mesh;
            lod.sharedMaterials = c.GetTpsMaterialsLOD();
            SetActiveSafe(lod, true);
        }
    }

    private static void SyncHelmet(TPSRigPreviewManager tester)
    {
        var h = GetHelmet(s_helmetID);
        if (h == null) { s_helmetID = 0; DespawnHelmet(); return; }
        if (tester.headgear_pos == null)
        {
            Debug.LogWarning("[TPS Tester] headgear_pos is not assigned on TPSRigPreviewManager.");
            return;
        }

        int sourceHash = ComputeRendererHash(h.gameObject);
        if (s_spawnedHelmet == null || sourceHash != s_spawnedHelmetSourceHash)
        {
            DespawnHelmet();
            GameObject inst = Object.Instantiate(h.gameObject);
            inst.name = "[TESTER] " + h.gameObject.name;
            inst.transform.SetParent(tester.headgear_pos, false);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            EnforceDontSaveFlags(inst);
            s_spawnedHelmet = inst;
            s_spawnedHelmetSourceHash = sourceHash;
        }
    }

    private static int ComputeRendererHash(GameObject src)
    {
        unchecked
        {
            int hash = 17;
            var renderers = src.GetComponentsInChildren<Renderer>(true);
            hash = hash * 31 + renderers.Length;
            foreach (var r in renderers)
            {
                Mesh m = null;
                if (r is SkinnedMeshRenderer smr) m = smr.sharedMesh;
                else
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null) m = mf.sharedMesh;
                }
                hash = hash * 31 + (m != null ? m.GetInstanceID() : 0);
                var mats = r.sharedMaterials;
                hash = hash * 31 + (mats != null ? mats.Length : 0);
                if (mats != null)
                    foreach (var mat in mats)
                        hash = hash * 31 + (mat != null ? mat.GetInstanceID() : 0);
            }
            return hash;
        }
    }

    private static void DespawnHelmet()
    {
        if (s_spawnedHelmet != null)
            Object.DestroyImmediate(s_spawnedHelmet);
        s_spawnedHelmet = null;
        s_spawnedHelmetSourceHash = 0;
    }

    private static void ClearUniformOnTester()
    {
        var tester = FindTester();
        if (tester == null) return;
        ClearRenderer(tester.uniform);
        ClearRenderer(tester.uniform_lod);
        ClearRenderer(tester.uniform_fps);
    }

    private static void ClearRendererPair(SkinnedMeshRenderer a, SkinnedMeshRenderer b)
    {
        ClearRenderer(a);
        ClearRenderer(b);
    }

    private static void ClearRenderer(SkinnedMeshRenderer r)
    {
        if (r == null) return;
        r.sharedMesh = null;
        r.sharedMaterials = new Material[0];
    }

    private static void SetActiveSafe(SkinnedMeshRenderer smr, bool active)
    {
        if (smr != null) smr.gameObject.SetActive(active);
    }

    private static void RepaintRelevantInspectors()
    {
        foreach (var ed in Resources.FindObjectsOfTypeAll<Editor>())
        {
            if (ed != null && (ed.target is ItemClothing || ed.target is ItemHelmet))
                ed.Repaint();
        }
    }

    // ===== Banner helper used by editors =====

    public static void DrawLinkedBanner(string text)
    {
        var rect = EditorGUILayout.GetControlRect(false, 26);
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.55f, 0.18f, 0.95f));
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), new Color(0.2f, 1f, 0.25f));
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), new Color(0.2f, 1f, 0.25f));
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.Label(rect, text, style);
    }
}
#endif