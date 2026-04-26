using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

public class TPSRigPreviewManager : MonoBehaviour
{
    public SkinnedMeshRenderer uniform;
    public SkinnedMeshRenderer uniform_fps;
    public SkinnedMeshRenderer uniform_lod;
    public SkinnedMeshRenderer vest;
    public SkinnedMeshRenderer vest_lod;
    public SkinnedMeshRenderer body;
    public SkinnedMeshRenderer body_fps;
    public Transform headgear_pos;

    public Mesh fps_longsleeve;
    public Mesh fps_shortleeve;
    public Mesh tps_longsleeve_longpants;
    public Mesh tps_longsleeve_shortpants;
    public Mesh tps_shortleeve_longpants;
    public Mesh tps_shortleeve_shortpants;


#if UNITY_EDITOR
    public static TPSRigPreviewManager GetTpsRigtester()
    {
        TPSRigPreviewManager tester = GameObject.FindObjectOfType<TPSRigPreviewManager>();
        if (tester == null)
        {
            GameObject animRoot = (GameObject)PrefabUtility.InstantiatePrefab(EditorAssetFinder.Find<GameObject>("tps rig tester"));
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

    /*[MenuItem("Assets/ER2 TOOLS/Tools/RIG Testing/Test rigged mesh", false, 2004)]
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
                    if (TestBoneOrder(originalMeshRenderer, tester.body, "Head"))
                    {
                        tester.body.sharedMesh = originalMeshRenderer.sharedMesh;
                        tester.body.sharedMaterials = originalMeshRenderer.sharedMaterials;
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
    }*/

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
public static class TPSRigTesterManager
{
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
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        UnityEditor.SceneManagement.EditorSceneManager.sceneOpening += OnSceneOpening;

        // Clean any leftover testers/checkers from a previous editor state
        EditorApplication.delayCall += () =>
        {
            DisableTester();
            DestroyAllPlacementCheckers();
        };
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            DisableTester();
            DestroyAllPlacementCheckers();
        }
    }

    private static void OnBeforeAssemblyReload()
    {
        DisableTester();
        DestroyAllPlacementCheckers();
    }

    private static void OnSceneOpening(string path, UnityEditor.SceneManagement.OpenSceneMode mode)
    {
        DisableTester();
        DestroyAllPlacementCheckers();
    }

    private static void DestroyAllPlacementCheckers()
    {
        DestroyAllOfType<HandsPlacementChecker>();
        DestroyAllOfType<HandsPlacementCheckerSteeringWheel>();
        DestroyAllOfType<HeadPlacementChecker>();
    }

    private static void DestroyAllOfType<T>() where T : MonoBehaviour
    {
        var all = Resources.FindObjectsOfTypeAll<T>();
        foreach (var c in all)
        {
            if (c == null) continue;
            if (EditorUtility.IsPersistent(c)) continue;
            if (c.gameObject == null) continue;
            Object.DestroyImmediate(c.gameObject);
        }
    }

    // ===== Tester lifecycle =====

    public static TPSRigPreviewManager FindTester()
    {
        s_cachedTester = null;
        TPSRigPreviewManager first = null;
        var all = Resources.FindObjectsOfTypeAll<TPSRigPreviewManager>();
        foreach (var t in all)
        {
            if (t == null) continue;
            if (EditorUtility.IsPersistent(t)) continue;
            if (t.gameObject == null) continue;

            if (first == null)
                first = t;
            else
                Object.DestroyImmediate(t.gameObject);
        }
        s_cachedTester = first;
        return first;
    }

    public static TPSRigPreviewManager GetOrSpawnTester(Transform pos)
    {
        var existing = FindTester();
        if (existing != null)
        {
            EnforceDontSaveFlags(existing.gameObject);
            return existing;
        }

        var prefab = EditorAssetFinder.Find<GameObject>("tps rig tester");
        if (prefab == null)
        {
            Debug.LogError($"[TPS Tester] Prefab not found.");
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (go == null) return null;

        go.transform.localPosition = Vector3.zero;
        go.transform.localEulerAngles = Vector3.zero;
        go.transform.localScale = Vector3.one;

        EnforceDontSaveFlags(go);
        s_cachedTester = go.GetComponent<TPSRigPreviewManager>();

        bool isPrefabAsset = pos != null && EditorUtility.IsPersistent(pos.gameObject);
        if (isPrefabAsset)
            PositionInFrontOfCamera(s_cachedTester);
        else
            PositionAbove(s_cachedTester, pos);

        return s_cachedTester;
    }

    private static void PositionInFrontOfCamera(TPSRigPreviewManager tester)
    {
        if (tester == null) return;
        SceneView sv = SceneView.lastActiveSceneView;
        if (sv != null && sv.camera != null)
        {
            Vector3 camPos = sv.camera.transform.position;
            Vector3 fwd = sv.camera.transform.forward;
            tester.transform.position = camPos + fwd * 3f;
            // face the camera
            Vector3 lookDir = -fwd; lookDir.y = 0;
            if (lookDir.sqrMagnitude < 0.0001f) lookDir = Vector3.forward;
            tester.transform.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }
        else
        {
            tester.transform.position = Vector3.zero;
            tester.transform.rotation = Quaternion.identity;
        }
    }

    private static void EnforceDontSaveFlags(GameObject root)
    {
        var flags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy;
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
        if (c == null) return;
        var tester = GetOrSpawnTester(c.transform);
        if (tester == null) return;
        s_uniformID = c.GetInstanceID();
        s_uniformSleeveMode = sleeveMode;
        s_lastSync = 0;
        SyncAll();
        SceneView.RepaintAll();
        RepaintRelevantInspectors();
    }

    public static void LinkGear(ItemClothing c)
    {
        if (c == null || c.mesh == null) return;
        var tester = GetOrSpawnTester(c.transform);
        if (tester == null) return;
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
        if (h == null) return;
        var tester = GetOrSpawnTester(h.transform);
        if (tester == null) return;
        s_helmetID = h.GetInstanceID();
        s_lastSync = 0;
        SyncAll();
        SceneView.RepaintAll();
        RepaintRelevantInspectors();
    }
    private static void PositionAbove(TPSRigPreviewManager tester, Transform target)
    {
        if (tester == null || target == null) return;
        tester.transform.position = target.position - Vector3.forward * .5f;
        tester.transform.rotation = Quaternion.identity;
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

        // Pick body skin meshes that complement what the uniform exposes
        Mesh tpsBodyMesh = null;
        Mesh fpsBodyMesh = null;
        bool hideFpsBody = false;
        bool hideHead = false;
        switch (handsMode)
        {
            case UniformSleeveCoverType.longSleeve:
                tpsBodyMesh = tester.tps_longsleeve_longpants;
                fpsBodyMesh = tester.fps_longsleeve;
                break;
            case UniformSleeveCoverType.shortSleeve:
                tpsBodyMesh = tester.tps_shortleeve_longpants;
                fpsBodyMesh = tester.fps_shortleeve;
                break;
            case UniformSleeveCoverType.coversSleeveAndHand:
                hideFpsBody = true;
                hideHead = true;
                break;
            case UniformSleeveCoverType.shortSleeveAndPants:
                tpsBodyMesh = tester.tps_shortleeve_shortpants;
                fpsBodyMesh = tester.fps_shortleeve;
                break;
            case UniformSleeveCoverType.longSleeveShortPants:
                tpsBodyMesh = tester.tps_longsleeve_shortpants;
                fpsBodyMesh = tester.fps_longsleeve;
                break;
        }

        if (tester.body != null && tpsBodyMesh != null)
            tester.body.sharedMesh = tpsBodyMesh;
        if (tester.body_fps != null && fpsBodyMesh != null)
            tester.body_fps.sharedMesh = fpsBodyMesh;

        SetActiveSafe(tester.body_fps, !hideFpsBody);

        SetActiveSafe(tester.body, !hideHead);
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