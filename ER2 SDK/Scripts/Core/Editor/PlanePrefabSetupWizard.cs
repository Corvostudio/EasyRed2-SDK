#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace Corvostudio.EditorTools
{
    public class PlanePrefabSetupWizard : EditorWindow
    {
        const string TEMPLATE_PREF = "Corvostudio.PlaneWizard.TemplatePath";
        const string MENU_PATH = "ER2 TOOLS/Tools/Vehicles/Plane Prefab Setup Wizard";

        // ----- Template -----
        GameObject templatePrefab;

        // ----- Detachable parts (multi-select) -----
        readonly List<Transform> wingLeftMeshes = new List<Transform>();
        readonly List<Transform> wingRightMeshes = new List<Transform>();
        readonly List<Transform> tailMeshes = new List<Transform>();
        readonly List<Transform> propellerMeshes = new List<Transform>();
        // (no fuselage slot: it's "everything else")

        // ----- Wheels (single) -----
        Transform wheelLeftMesh;
        Transform wheelRightMesh;
        Transform tailWheelMesh;

        // ----- Flaps + steering (single) -----
        Transform aileronLeftT;
        Transform aileronRightT;
        Transform elevatorT;
        Transform rudderT;
        Transform planeSteerT;

        // ----- UI state -----
        Vector2 scroll;
        bool showHelp = true;

        [MenuItem(MENU_PATH)]
        public static void ShowWindow()
        {
            var w = GetWindow<PlanePrefabSetupWizard>("Plane Prefab Setup");
            w.minSize = new Vector2(440, 720);
            w.LoadTemplate();
        }

        // ===================== UI =====================
        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHelp();
            DrawTemplate();
            EditorGUILayout.Space(8);
            DrawDetachableSlots();
            EditorGUILayout.Space(8);
            DrawWheelSlots();
            EditorGUILayout.Space(8);
            DrawFlapSlots();
            EditorGUILayout.Space(12);
            DrawGenerate();
            EditorGUILayout.EndScrollView();
        }

        void DrawHelp()
        {
            showHelp = EditorGUILayout.Foldout(showHelp, "How to use", true);
            if (!showHelp) return;
            EditorGUILayout.HelpBox(
                "1. Drop your imported FBX into the scene. Keep all mesh GameObjects as direct children of the root (LOD variants either as children of the LOD0 mesh OR as siblings - both naming conventions '.001/.002' or '_LODn' are supported).\n" +
                "2. Set a Template plane (BF-109 by default) - used to read default values and to clone MainTurretData / internal parts.\n" +
                "3. For each detachable part (Wing Left/Right, Tail, Propeller) select the meshes in the Hierarchy and click Capture. The first selected becomes the [primary] - container is positioned at its world position.\n" +
                "4. For each wheel slot capture the wheel mesh - the WheelCollider goes directly on it.\n" +
                "5. For Aileron L/R, Elevator, Rudder, PlaneSteer slot capture the single transform that should rotate (usually a pivot or the mesh itself).\n" +
                "6. Anything not captured stays at root level and gets the fuselage LOD.\n" +
                "7. Click GENERATE. The wizard resets the root to identity rotation/scale=1 (preserving children world transforms), creates containers, copies values from template, and wires up VehiclePlane.\n" +
                "8. After generation: position MainTurretData/internal parts/Seat0, configure weapons/ammo, and tune flap axisFlap if needed.",
                MessageType.Info);
        }

        void DrawTemplate()
        {
            EditorGUI.BeginChangeCheck();
            templatePrefab = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Template plane", "Reference plane prefab (e.g. BF-109) used to read default values: VehiclePlane params, VDP, MainTurretData, internal parts."),
                templatePrefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetString(TEMPLATE_PREF, AssetDatabase.GetAssetPath(templatePrefab));
        }

        public void LoadTemplate()
        {
            var path = EditorPrefs.GetString(TEMPLATE_PREF, "");
            if (!string.IsNullOrEmpty(path))
                templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (templatePrefab == null)
            {
                var guids = AssetDatabase.FindAssets("BF-109 t:Prefab");
                foreach (var g in guids)
                {
                    var p = AssetDatabase.GUIDToAssetPath(g);
                    if (!p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) continue;
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                    if (go != null) { templatePrefab = go; EditorPrefs.SetString(TEMPLATE_PREF, p); break; }
                }
            }
        }

        void DrawDetachableSlots()
        {
            GUILayout.Label("Detachable parts (multi-select)", EditorStyles.boldLabel);
            DrawMulti("Wing Left", wingLeftMeshes);
            DrawMulti("Wing Right", wingRightMeshes);
            DrawMulti("Tail", tailMeshes);
            DrawMulti("Propeller", propellerMeshes);
        }

        void DrawMulti(string label, List<Transform> list)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label + $" ({list.Count})", EditorStyles.boldLabel, GUILayout.Width(140));
                if (GUILayout.Button("Capture", GUILayout.Width(70))) CaptureMulti(list, replace: true);
                if (GUILayout.Button("Add", GUILayout.Width(50))) CaptureMulti(list, replace: false);
                if (GUILayout.Button("Clear", GUILayout.Width(50))) list.Clear();
            }
            for (int i = 0; i < list.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var t = list[i];
                    GUILayout.Label((i == 0 ? "[primary] " : "") + (t ? t.name : "<missing>"));
                    GUILayout.FlexibleSpace();
                    if (i > 0 && GUILayout.Button("\u2191", GUILayout.Width(25)))
                    {
                        var tmp = list[0]; list[0] = list[i]; list[i] = tmp;
                    }
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        list.RemoveAt(i); i--;
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        void CaptureMulti(List<Transform> list, bool replace)
        {
            if (replace) list.Clear();
            foreach (var go in Selection.gameObjects)
                if (go != null && !list.Contains(go.transform)) list.Add(go.transform);
        }

        void DrawWheelSlots()
        {
            GUILayout.Label("Wheels (single)", EditorStyles.boldLabel);
            DrawSingle("Wheel Left", ref wheelLeftMesh);
            DrawSingle("Wheel Right", ref wheelRightMesh);
            DrawSingle("Tail Wheel (opt)", ref tailWheelMesh);
        }

        void DrawFlapSlots()
        {
            GUILayout.Label("Flaps + Steering (single)", EditorStyles.boldLabel);
            DrawSingle("Aileron L", ref aileronLeftT);
            DrawSingle("Aileron R", ref aileronRightT);
            DrawSingle("Elevator", ref elevatorT);
            DrawSingle("Rudder", ref rudderT);
            DrawSingle("PlaneSteer", ref planeSteerT);
        }

        void DrawSingle(string label, ref Transform t)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(label, GUILayout.Width(140));
                t = (Transform)EditorGUILayout.ObjectField(t, typeof(Transform), true);
                if (GUILayout.Button("Capture", GUILayout.Width(70)))
                {
                    if (Selection.activeGameObject != null)
                        t = Selection.activeGameObject.transform;
                }
            }
        }

        void DrawGenerate()
        {
            using (new EditorGUI.DisabledScope(!CanGenerate(out string err)))
            {
                if (GUILayout.Button("GENERATE", GUILayout.Height(38))) Generate();
                if (!CanGenerate(out err))
                    EditorGUILayout.HelpBox(err, MessageType.Info);
            }
        }

        bool CanGenerate(out string reason)
        {
            bool hasAny =
                wingLeftMeshes.Count > 0 || wingRightMeshes.Count > 0 ||
                tailMeshes.Count > 0 || propellerMeshes.Count > 0 ||
                wheelLeftMesh || wheelRightMesh || tailWheelMesh ||
                aileronLeftT || aileronRightT || elevatorT || rudderT || planeSteerT;
            if (!hasAny)
            {
                reason = "Capture at least one element to determine the scene root.";
                return false;
            }
            reason = null;
            return true;
        }

        // ===================== GENERATE =====================
        void Generate()
        {
            try
            {
                var sceneRoot = FindSceneRoot();
                if (sceneRoot == null)
                {
                    EditorUtility.DisplayDialog("Plane Wizard", "Could not find scene root.", "OK");
                    return;
                }

                Undo.RegisterFullObjectHierarchyUndo(sceneRoot.gameObject, "Plane Composition");
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Plane composition");

                // 1. Flatten root (rotation->identity, scale->1, preserving children world)
                FlattenRoot(sceneRoot);

                // 2. Resolve script types via reflection
                var vpType = FindTypeByName("VehiclePlane");
                var vdpType = FindTypeByName("VehicleDamagableDetachablePart");
                var internalPartType = FindTypeByName("InternalPart") ?? FindTypeByName("VehicleInternalPart");
                var propellerType = FindTypeByName("Propeller");
                var flapType = FindTypeByName("FlapRotator");
                var steerType = FindTypeByName("PlaneSteer");
                var wheelType = templatePrefab != null ? FindWheelScriptType(templatePrefab) : FindTypeByName("Wheel");
                var seatType = templatePrefab != null ? FindSeatScriptType(templatePrefab) : null;

                // 3. Copy root components from template FIRST so VehiclePlane exists
                //    (flap/steer/propeller need to reference it via connectedPlane).
                //    Refs inside VehiclePlane still point to template at this stage; we fix them in WireUp at step 13.
                CopyRootComponentsFromTemplate(sceneRoot.gameObject);

                // 4. Read template VDP references for copy
                var tmplVDPs = ReadTemplateVDPs();

                // 5. Create part containers (identity rot/scale, positioned at primary mesh world pos)
                Transform cWingL = CreateContainer("WingL", sceneRoot, wingLeftMeshes, vdpType, tmplVDPs.wingL);
                Transform cWingR = CreateContainer("WingR", sceneRoot, wingRightMeshes, vdpType, tmplVDPs.wingR);
                Transform cTail = CreateContainer("Tail", sceneRoot, tailMeshes, vdpType, tmplVDPs.tail);
                Transform cProp = CreateContainer("Propeller", sceneRoot, propellerMeshes, vdpType, tmplVDPs.prop);

                // 6. Setup Propeller controller script
                if (cProp != null && propellerType != null)
                    SetupPropellerScript(cProp, propellerType, sceneRoot);

                // 7. Setup wheels (WheelCollider+script on separate sibling GameObject)
                var brakingWheelColliders = new List<UObject>();
                if (wheelLeftMesh)
                {
                    var wc = SetupWheel(wheelLeftMesh, side: 0, wheelType, "w_l_front_up");
                    if (wc != null) brakingWheelColliders.Add(wc);
                }
                if (wheelRightMesh)
                {
                    var wc = SetupWheel(wheelRightMesh, side: 1, wheelType, "w_r_front_up");
                    if (wc != null) brakingWheelColliders.Add(wc);
                }
                if (tailWheelMesh)
                {
                    SetupWheel(tailWheelMesh, side: 0, wheelType, "w_rear_up", isTail: true);
                }

                // 8. Clone MainTurretData and internal parts from template (Marco fine-tunes after)
                Component clonedTurretScript = null;
                var clonedInternalParts = new List<Component>();
                if (templatePrefab != null)
                {
                    clonedTurretScript = CloneMainTurretDataFromTemplate(sceneRoot);
                    clonedInternalParts = CloneInternalPartsFromTemplate(sceneRoot, internalPartType);
                }

                // 9. Setup flap rotators (script on a NEW pivot at identity rotation, mesh becomes child of pivot)
                var wizardPivots = new HashSet<Transform>();
                if (flapType != null)
                {
                    if (aileronLeftT) SetupFlap(aileronLeftT, flapType, FlapKind.AileronL, sceneRoot, wizardPivots);
                    if (aileronRightT) SetupFlap(aileronRightT, flapType, FlapKind.AileronR, sceneRoot, wizardPivots);
                    if (elevatorT) SetupFlap(elevatorT, flapType, FlapKind.Elevator, sceneRoot, wizardPivots);
                    if (rudderT) SetupFlap(rudderT, flapType, FlapKind.Rudder, sceneRoot, wizardPivots);
                }

                // 10. Setup PlaneSteer (script on a NEW pivot at identity rotation)
                Transform steerPivot = null;
                if (steerType != null && planeSteerT != null)
                    steerPivot = SetupPlaneSteer(planeSteerT, steerType, sceneRoot, wizardPivots);

                // 11. Create or clone Seat0
                Transform seat0 = sceneRoot.Find("Seat0");
                Component seatComp = null;
                if (seat0 == null)
                {
                    seat0 = CreateOrCloneSeat0(sceneRoot, seatType, out seatComp);
                }
                else if (seatType != null)
                {
                    seatComp = seat0.GetComponent(seatType);
                }

                // 12. Wire VehiclePlane to all the new references
                WireUpVehiclePlane(sceneRoot.gameObject, vpType, cWingL, cWingR, cTail, cProp,
                                   brakingWheelColliders, clonedInternalParts,
                                   seat0, seatComp, clonedTurretScript, steerPivot ?? planeSteerT);

                // 13. Setup root LODGroup for fuselage (everything not in containers/wheels)
                SetupRootFuselageLOD(sceneRoot, wizardPivots, cWingL, cWingR, cTail, cProp);

                Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
                Selection.activeGameObject = sceneRoot.gameObject;
                EditorGUIUtility.PingObject(sceneRoot);
                Debug.Log("[PlaneWizard] Generation done. Fine-tune positions of Seat0/MainTurretData/internal parts and configure weapons/animations manually.");
            }
            catch (Exception e)
            {
                Debug.LogError("[PlaneWizard] Errore: " + e);
            }
        }

        // ===================== ROOT FLATTEN =====================
        Transform FindSceneRoot()
        {
            Transform first = null;
            if (wingLeftMeshes.Count > 0) first = wingLeftMeshes[0];
            else if (wingRightMeshes.Count > 0) first = wingRightMeshes[0];
            else if (tailMeshes.Count > 0) first = tailMeshes[0];
            else if (propellerMeshes.Count > 0) first = propellerMeshes[0];
            else if (wheelLeftMesh) first = wheelLeftMesh;
            else if (wheelRightMesh) first = wheelRightMesh;
            else if (tailWheelMesh) first = tailWheelMesh;
            else if (aileronLeftT) first = aileronLeftT;
            else if (aileronRightT) first = aileronRightT;
            else if (elevatorT) first = elevatorT;
            else if (rudderT) first = rudderT;
            else if (planeSteerT) first = planeSteerT;
            return first ? first.root : null;
        }

        // Reset root rotation to identity and scale to 1 while preserving children's WORLD transforms
        void FlattenRoot(Transform root)
        {
            if (root == null) return;
            if (root.localRotation == Quaternion.identity && root.localScale == Vector3.one) return;

            // Cache children
            var children = new List<Transform>();
            for (int i = 0; i < root.childCount; i++) children.Add(root.GetChild(i));

            // SetParent(null, true) preserves world transform
            foreach (var c in children) Undo.SetTransformParent(c, null, "Detach for flatten");

            // Reset root local
            Undo.RecordObject(root, "Flatten root");
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            // Reparent (worldPositionStays=true preserves world)
            foreach (var c in children) Undo.SetTransformParent(c, root, "Reparent after flatten");
        }

        // ===================== CONTAINER CREATION =====================
        Transform CreateContainer(string name, Transform sceneRoot, List<Transform> meshes, Type vdpType, Component templateVDP)
        {
            if (meshes == null || meshes.Count == 0) return null;
            var primary = meshes[0];
            if (primary == null) return null;

            // Identity rotation/scale, positioned at primary's world pos
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(sceneRoot, false);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.transform.position = primary.position;

            // Reparent meshes preserving world
            foreach (var m in meshes)
            {
                if (m == null) continue;
                Undo.SetTransformParent(m, go.transform, "Reparent to " + name);
            }

            // Add VDP and copy from template (if any)
            if (vdpType != null)
            {
                var vdp = GetOrAdd(go, vdpType);
                if (templateVDP != null && vdp != null)
                {
                    try { EditorUtility.CopySerialized(templateVDP, vdp); }
                    catch (Exception e) { Debug.LogWarning($"[PlaneWizard] CopySerialized VDP {name}: {e.Message}"); }
                }
            }

            // Build LODGroup from naming
            SetupContainerLOD(go.transform);

            // Auto-fit BoxCollider
            AddBoundsBoxCollider(go.transform);

            return go.transform;
        }

        void SetupContainerLOD(Transform container)
        {
            // LOD level = number of ANCESTORS (between this renderer and the container)
            // that themselves have a Renderer. Empty parents (pivots, structural groups) don't count.
            // This way:
            //   container > Left_wing (renderer)         -> 0 ancestor renderers -> LOD0
            //   container > Left_wing > Left_wing.001    -> 1 ancestor renderer  -> LOD1
            //   container > pivot(no renderer) > mesh    -> 0 ancestor renderers -> LOD0
            var renderersByLOD = new Dictionary<int, List<Renderer>>();
            foreach (var r in container.GetComponentsInChildren<Renderer>(true))
            {
                int lod = LODLevelByAncestorRenderers(r.transform, container);
                if (!renderersByLOD.TryGetValue(lod, out var list))
                    renderersByLOD[lod] = list = new List<Renderer>();
                list.Add(r);
            }
            if (renderersByLOD.Count == 0) return;

            var grp = GetOrAdd<LODGroup>(container.gameObject);
            var thresholds = new[] { 0.5f, 0.1f, 0.02f, 0.005f };
            var lods = new List<LOD>();
            foreach (var kv in renderersByLOD.OrderBy(k => k.Key))
            {
                int idx = Mathf.Min(kv.Key, thresholds.Length - 1);
                lods.Add(new LOD(thresholds[idx], kv.Value.ToArray()));
            }
            grp.SetLODs(lods.ToArray());
            grp.RecalculateBounds();
        }

        // Count how many ancestors (up to but excluding `stopAt`) have a Renderer component.
        // Used as the LOD level: each renderer-bearing ancestor pushes us one LOD level deeper.
        int LODLevelByAncestorRenderers(Transform r, Transform stopAt)
        {
            int lod = 0;
            var p = r.parent;
            while (p != null && p != stopAt)
            {
                if (p.GetComponent<Renderer>() != null) lod++;
                p = p.parent;
            }
            return lod;
        }

        int DepthFromAncestor(Transform t, Transform ancestor)
        {
            int d = 0;
            var p = t;
            while (p != null && p != ancestor) { d++; p = p.parent; }
            return d;
        }

        void AddBoundsBoxCollider(Transform container)
        {
            if (container == null) return;
            var renderers = container.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;

            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);

            var bc = GetOrAdd<BoxCollider>(container.gameObject);
            bc.center = container.InverseTransformPoint(b.center);
            var ls = container.lossyScale;
            bc.size = new Vector3(
                ls.x != 0 ? b.size.x / Mathf.Abs(ls.x) : b.size.x,
                ls.y != 0 ? b.size.y / Mathf.Abs(ls.y) : b.size.y,
                ls.z != 0 ? b.size.z / Mathf.Abs(ls.z) : b.size.z);
        }

        // ===================== PROPELLER =====================
        void SetupPropellerScript(Transform container, Type propellerType, Transform sceneRoot)
        {
            var script = GetOrAdd(container.gameObject, propellerType);
            if (script == null) return;

            // Detect propeller_slow / propeller_fast among container children by name
            Transform slow = null, fast = null;
            for (int i = 0; i < container.childCount; i++)
            {
                var c = container.GetChild(i);
                var n = c.name.ToLowerInvariant();
                if (fast == null && (n.Contains("blur") || n.Contains("fast"))) fast = c;
                else if (slow == null) slow = c;
            }
            if (slow == null && container.childCount > 0) slow = container.GetChild(0);

            var so = new SerializedObject(script);
            TrySetVector3(so, "rotationAxis", new Vector3(0, 0, 1));
            TrySetObject(so, "propeller_slow", slow);
            TrySetObject(so, "propeller_fast", fast);
            TrySetObject(so, "connectedPlane", FindVehiclePlaneOnRoot(sceneRoot));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ===================== WHEELS =====================
        Type FindWheelScriptType(GameObject template)
        {
            foreach (var wc in template.GetComponentsInChildren<WheelCollider>(true))
            {
                foreach (var c in wc.GetComponents<Component>())
                {
                    if (c == null || c is Transform || c is WheelCollider) continue;
                    return c.GetType();
                }
            }
            return null;
        }

        WheelCollider SetupWheel(Transform wheelMesh, int side, Type wheelScriptType, string colliderGoName, bool isTail = false)
        {
            if (wheelMesh == null) return null;

            // The WheelCollider + Wheel script live on a SEPARATE GameObject, sibling of the wheel mesh.
            // This way the wheel mesh (visual) can move/rotate independently of the WheelCollider (physics).
            var parent = wheelMesh.parent != null ? wheelMesh.parent : wheelMesh.root;

            // Avoid duplicate creation if a sibling with the same name already exists
            Transform existingGo = null;
            for (int i = 0; i < parent.childCount; i++)
            {
                var c = parent.GetChild(i);
                if (c.name == colliderGoName) { existingGo = c; break; }
            }

            GameObject go;
            if (existingGo != null)
            {
                go = existingGo.gameObject;
            }
            else
            {
                go = new GameObject(colliderGoName);
                Undo.RegisterCreatedObjectUndo(go, "Create wheel collider GO");
                go.transform.SetParent(parent, false);
                // Position at wheel mesh world position, identity rotation (so WheelCollider Y axis = world up)
                go.transform.position = wheelMesh.position;
                go.transform.rotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }

            var wc = GetOrAdd<WheelCollider>(go);
            wc.mass = 20f;
            wc.radius = 0.45f;
            wc.wheelDampingRate = 0.25f;
            wc.suspensionDistance = 0.3f;
            wc.forceAppPointDistance = 0f;
            wc.center = Vector3.zero;
            var spring = wc.suspensionSpring;
            spring.spring = 55000f; spring.damper = 5500f; spring.targetPosition = 0.5f;
            wc.suspensionSpring = spring;
            var fwd = wc.forwardFriction;
            fwd.extremumSlip = 0.4f; fwd.extremumValue = 1f; fwd.asymptoteSlip = 0.8f; fwd.asymptoteValue = 0.5f; fwd.stiffness = 0.3f;
            wc.forwardFriction = fwd;
            var sw = wc.sidewaysFriction;
            sw.extremumSlip = 0.4f; sw.extremumValue = 1f; sw.asymptoteSlip = 0.8f; sw.asymptoteValue = 0.5f; sw.stiffness = 0.3f;
            wc.sidewaysFriction = sw;

            if (wheelScriptType != null && go.GetComponent(wheelScriptType) == null)
            {
                var wheelScript = go.AddComponent(wheelScriptType);
                var so = new SerializedObject(wheelScript);
                //TryGetProp(so, "antiRollBar")?.SetValue(true);
                TryGetProp(so, "steer_angle")?.SetValue(0f);
                TrySetByte(so, "side", (byte)side);
                var wt = so.FindProperty("wheelsTransform");
                if (wt != null && wt.isArray)
                {
                    wt.arraySize = 1;
                    wt.GetArrayElementAtIndex(0).objectReferenceValue = wheelMesh;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return wc;
        }

        // ===================== FLAPS / STEERING =====================
        enum FlapKind { AileronL, AileronR, Elevator, Rudder }

        // Wrap a transform in a NEW parent GameObject that has identity local rotation/scale.
        // The wrapped transform becomes a child of the pivot (worldPositionStays preserved).
        // Used for flap/steer scripts that need a clean rotation reference.
        Transform WrapInPivot(Transform t, string pivotName)
        {
            if (t == null) return null;
            var oldParent = t.parent;
            Vector3 worldPos = t.position;

            var pivot = new GameObject(pivotName);
            Undo.RegisterCreatedObjectUndo(pivot, "Create pivot " + pivotName);
            pivot.transform.SetParent(oldParent, false);
            pivot.transform.position = worldPos;
            pivot.transform.localRotation = Quaternion.identity;
            pivot.transform.localScale = Vector3.one;

            Undo.SetTransformParent(t, pivot.transform, "Wrap " + t.name + " in pivot");
            return pivot.transform;
        }

        void SetupFlap(Transform t, Type flapType, FlapKind kind, Transform sceneRoot, HashSet<Transform> wizardPivots)
        {
            // Wrap the captured transform in an identity-rotation pivot, put script on the pivot
            string pivotName = kind switch
            {
                FlapKind.AileronL => "AileronLeft_pivot",
                FlapKind.AileronR => "AileronRight_pivot",
                FlapKind.Elevator => "Elevator_pivot",
                FlapKind.Rudder => "Rudder_pivot",
                _ => t.name + "_pivot"
            };
            var pivot = WrapInPivot(t, pivotName);
            if (pivot == null) return;
            if (wizardPivots != null) wizardPivots.Add(pivot);

            var script = GetOrAdd(pivot.gameObject, flapType);
            if (script == null) return;

            int axisDirection, axisFlap;
            float rotationMultiplier, rotationClamp;
            switch (kind)
            {
                case FlapKind.AileronL: axisDirection = 1; axisFlap = 0; rotationMultiplier = 25f; rotationClamp = 25f; break;
                case FlapKind.AileronR: axisDirection = 1; axisFlap = 0; rotationMultiplier = -25f; rotationClamp = 25f; break;
                case FlapKind.Elevator: axisDirection = 0; axisFlap = 0; rotationMultiplier = 45f; rotationClamp = 35f; break;
                case FlapKind.Rudder: axisDirection = 2; axisFlap = 2; rotationMultiplier = -50f; rotationClamp = 50f; break;
                default: axisDirection = 0; axisFlap = 0; rotationMultiplier = 45f; rotationClamp = 35f; break;
            }

            var so = new SerializedObject(script);
            TrySetObject(so, "connectedPlane", FindVehiclePlaneOnRoot(sceneRoot));
            TrySetEnumInt(so, "axisDirection", axisDirection);
            TrySetByte(so, "axisFlap", (byte)axisFlap);
            TrySetFloat(so, "rotationMultiplier", rotationMultiplier);
            TrySetFloat(so, "rotationClamp", rotationClamp);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Transform SetupPlaneSteer(Transform t, Type steerType, Transform sceneRoot, HashSet<Transform> wizardPivots)
        {
            // Wrap in identity-rotation pivot, put script on the pivot
            var pivot = WrapInPivot(t, "PlaneSteer_pivot");
            if (pivot == null) return null;
            if (wizardPivots != null) wizardPivots.Add(pivot);

            var script = GetOrAdd(pivot.gameObject, steerType);
            if (script == null) return pivot;
            var so = new SerializedObject(script);
            TrySetObject(so, "connectedPlane", FindVehiclePlaneOnRoot(sceneRoot));
            TrySetFloat(so, "rotationMultiplier", 0.4f);
            so.ApplyModifiedPropertiesWithoutUndo();
            return pivot;
        }

        // ===================== CLONE FROM TEMPLATE =====================
        Component CloneMainTurretDataFromTemplate(Transform sceneRoot)
        {
            if (templatePrefab == null) return null;
            var existing = sceneRoot.Find("MainTurretData");
            if (existing != null)
                return GetComponentByTypeName(existing, "Turret");

            var tmplGo = FindChildByName(templatePrefab.transform, "MainTurretData");
            if (tmplGo == null) return null;

            var clone = (GameObject)UObject.Instantiate(tmplGo, sceneRoot, false);
            clone.name = "MainTurretData";
            clone.transform.localPosition = tmplGo.transform.localPosition;
            clone.transform.localRotation = tmplGo.transform.localRotation;
            clone.transform.localScale = tmplGo.transform.localScale;
            Undo.RegisterCreatedObjectUndo(clone, "Clone MainTurretData");
            return GetComponentByTypeName(clone.transform, "Turret");
        }

        List<Component> CloneInternalPartsFromTemplate(Transform sceneRoot, Type internalPartType)
        {
            var result = new List<Component>();
            if (templatePrefab == null || internalPartType == null) return result;

            foreach (var mb in templatePrefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null || mb.GetType() != internalPartType) continue;
                var src = mb.gameObject;
                // Avoid duplicates: skip if a child with same name already exists
                if (sceneRoot.Find(src.name) != null) continue;

                var clone = (GameObject)UObject.Instantiate(src, sceneRoot, false);
                clone.name = src.name;
                clone.transform.localPosition = src.transform.localPosition;
                clone.transform.localRotation = src.transform.localRotation;
                clone.transform.localScale = src.transform.localScale;
                Undo.RegisterCreatedObjectUndo(clone, "Clone Internal Part " + src.name);
                var comp = clone.GetComponent(internalPartType);
                if (comp != null) result.Add(comp);
            }
            return result;
        }

        Type FindSeatScriptType(GameObject template)
        {
            var tmplSeat = FindChildByName(template.transform, "Seat0");
            if (tmplSeat == null) return null;
            foreach (var c in tmplSeat.GetComponents<Component>())
                if (c != null && !(c is Transform)) return c.GetType();
            return null;
        }

        Transform CreateOrCloneSeat0(Transform sceneRoot, Type seatType, out Component seatComp)
        {
            seatComp = null;
            // Try clone from template
            if (templatePrefab != null)
            {
                var tmplSeat = FindChildByName(templatePrefab.transform, "Seat0");
                if (tmplSeat != null)
                {
                    var clone = (GameObject)UObject.Instantiate(tmplSeat, sceneRoot, false);
                    clone.name = "Seat0";
                    clone.transform.localPosition = tmplSeat.transform.localPosition;
                    clone.transform.localRotation = tmplSeat.transform.localRotation;
                    clone.transform.localScale = tmplSeat.transform.localScale;
                    Undo.RegisterCreatedObjectUndo(clone, "Clone Seat0");
                    if (seatType != null) seatComp = clone.GetComponent(seatType);
                    return clone.transform;
                }
            }
            // Otherwise create new
            var go = new GameObject("Seat0");
            Undo.RegisterCreatedObjectUndo(go, "Create Seat0");
            go.transform.SetParent(sceneRoot, false);
            if (seatType != null) seatComp = go.AddComponent(seatType);
            return go.transform;
        }

        // ===================== ROOT COMPONENTS =====================
        // Skip these when copying from template:
        // - Transform (always)
        // - LODGroup (we rebuild it)
        // - Animation (template's clip references template's transforms)
        // - Camo (targetMeshes / showMeshes refs are template-specific)
        static readonly HashSet<string> SKIP_COPY_TYPENAMES = new HashSet<string> { "Animation", "LODGroup", "Camo" };

        void CopyRootComponentsFromTemplate(GameObject instance)
        {
            // Always ensure Rigidbody/AudioSource
            GetOrAdd<Rigidbody>(instance);
            GetOrAdd<AudioSource>(instance);

            if (templatePrefab == null) return;

            foreach (var src in templatePrefab.GetComponents<Component>())
            {
                if (src == null || src is Transform) continue;
                if (SKIP_COPY_TYPENAMES.Contains(src.GetType().Name)) continue;

                var t = src.GetType();
                var existing = instance.GetComponent(t);
                if (existing == null)
                {
                    try { existing = instance.AddComponent(t); }
                    catch (Exception e) { Debug.LogWarning($"[PlaneWizard] AddComponent {t.Name}: {e.Message}"); continue; }
                }
                if (existing != null)
                {
                    try { EditorUtility.CopySerialized(src, existing); }
                    catch (Exception e) { Debug.LogWarning($"[PlaneWizard] CopySerialized {t.Name}: {e.Message}"); }
                }
            }
        }

        // ===================== WIRE VEHICLE PLANE =====================
        void WireUpVehiclePlane(GameObject root, Type vpType,
                                Transform cWingL, Transform cWingR, Transform cTail, Transform cProp,
                                List<UObject> brakingWheels, List<Component> internalParts,
                                Transform seat0, Component seatComp, Component clonedTurret, Transform steeringWheel)
        {
            if (vpType == null) return;
            var vp = root.GetComponent(vpType);
            if (vp == null) vp = root.AddComponent(vpType);

            var so = new SerializedObject(vp);

            TrySetObject(so, "leftDetachableWing", GetVDPOn(cWingL));
            TrySetObject(so, "rightDetachableWing", GetVDPOn(cWingR));

            TrySetArrayElements(so, "detachableTails", cTail != null ? new UObject[] { GetVDPOn(cTail) } : null);
            TrySetArrayElements(so, "detachablePropellers", cProp != null ? new UObject[] { GetVDPOn(cProp) } : null);

            if (brakingWheels != null && brakingWheels.Count > 0)
                TrySetArrayElements(so, "brakingWheels", brakingWheels.ToArray());

            if (internalParts != null && internalParts.Count > 0)
                TrySetArrayElements(so, "internalParts", internalParts.Cast<UObject>().ToArray());

            // seats[0]
            if (seat0 != null)
            {
                var seatsProp = so.FindProperty("seats");
                if (seatsProp != null && seatsProp.isArray)
                {
                    if (seatsProp.arraySize == 0) seatsProp.arraySize = 1;
                    var first = seatsProp.GetArrayElementAtIndex(0);
                    SetSerObj(first, "seat", seatComp);
                    SetSerObj(first, "connectedTurret", clonedTurret);
                    SetSerBool(first, "isDriver", true);

                    if (steeringWheel != null)
                    {
                        var rIK = FindIKChild(steeringWheel, isLeft: false);
                        var lIK = FindIKChild(steeringWheel, isLeft: true);
                        SetSerObj(first, "rightHandIK", rIK);
                        SetSerObj(first, "lefthandIsIK", lIK);
                    }
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Transform FindIKChild(Transform parent, bool isLeft)
        {
            // Walk all descendants looking for "ik" + L/R indicator
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
            {
                if (t == parent) continue;
                var n = t.name.ToLowerInvariant();
                if (!n.Contains("ik")) continue;
                if (isLeft && (n.Contains("_l") || n.Contains("ikl") || n.EndsWith("l"))) return t;
                if (!isLeft && (n.Contains("_r") || n.Contains("ikr") || n.EndsWith("r"))) return t;
            }
            return null;
        }

        Component GetVDPOn(Transform t)
        {
            if (t == null) return null;
            foreach (var c in t.GetComponents<Component>())
                if (c != null && c.GetType().Name == "VehicleDamagableDetachablePart") return c;
            return null;
        }

        Component FindVehiclePlaneOnRoot(Transform sceneRoot)
        {
            if (sceneRoot == null) return null;
            foreach (var c in sceneRoot.GetComponents<Component>())
                if (c != null && c.GetType().Name == "VehiclePlane") return c;
            return null;
        }

        // ===================== ROOT FUSELAGE LOD =====================
        void SetupRootFuselageLOD(Transform sceneRoot, HashSet<Transform> wizardPivots, params Transform[] containers)
        {
            var skipRoots = new HashSet<Transform>();
            foreach (var c in containers) if (c != null) skipRoots.Add(c);

            var renderersByLOD = new Dictionary<int, List<Renderer>>();
            foreach (var r in sceneRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (IsDescendantOfAny(r.transform, skipRoots)) continue;

                int lod;
                // Renderers inside a wizard-created pivot (PlaneSteer / FlapRotator) are
                // control surfaces. They must end up at LOD0 of the fuselage regardless of
                // how deep the wrap puts them in the hierarchy.
                if (wizardPivots != null && IsDescendantOfAny(r.transform, wizardPivots))
                    lod = 0;
                else
                    lod = LODLevelByAncestorRenderers(r.transform, sceneRoot);

                if (!renderersByLOD.TryGetValue(lod, out var list))
                    renderersByLOD[lod] = list = new List<Renderer>();
                list.Add(r);
            }
            if (renderersByLOD.Count == 0) return;

            var grp = GetOrAdd<LODGroup>(sceneRoot.gameObject);
            var thresholds = new[] { 0.5f, 0.1f, 0.02f, 0.005f };
            var lods = new List<LOD>();
            foreach (var kv in renderersByLOD.OrderBy(k => k.Key))
            {
                int idx = Mathf.Min(kv.Key, thresholds.Length - 1);
                lods.Add(new LOD(thresholds[idx], kv.Value.ToArray()));
            }
            grp.SetLODs(lods.ToArray());
            grp.RecalculateBounds();

            // Add a BoxCollider on the root for fuselage hit detection.
            // VehicleImpact (copied from template) handles damage; without a collider on the root,
            // bullets pass through the fuselage without registering hits.
            // Bounds computed from LOD0 renderers (most accurate representation).
            if (renderersByLOD.TryGetValue(0, out var lod0Renderers) && lod0Renderers.Count > 0)
            {
                var b = lod0Renderers[0].bounds;
                for (int i = 1; i < lod0Renderers.Count; i++) b.Encapsulate(lod0Renderers[i].bounds);

                var bc = GetOrAdd<BoxCollider>(sceneRoot.gameObject);
                bc.center = sceneRoot.InverseTransformPoint(b.center);
                var ls = sceneRoot.lossyScale;
                bc.size = new Vector3(
                    ls.x != 0 ? b.size.x / Mathf.Abs(ls.x) : b.size.x,
                    ls.y != 0 ? b.size.y / Mathf.Abs(ls.y) : b.size.y,
                    ls.z != 0 ? b.size.z / Mathf.Abs(ls.z) : b.size.z);
            }
        }

        bool IsDescendantOfAny(Transform t, HashSet<Transform> ancestors)
        {
            var p = t;
            while (p != null) { if (ancestors.Contains(p)) return true; p = p.parent; }
            return false;
        }

        // ===================== TEMPLATE VDP REFS =====================
        struct TemplateVDPs
        {
            public Component wingL, wingR, tail, prop;
        }

        TemplateVDPs ReadTemplateVDPs()
        {
            var r = new TemplateVDPs();
            if (templatePrefab == null) return r;
            var vpType = FindTypeByName("VehiclePlane");
            if (vpType == null) return r;
            var vp = templatePrefab.GetComponent(vpType);
            if (vp == null) return r;
            var so = new SerializedObject(vp);
            r.wingL = so.FindProperty("leftDetachableWing")?.objectReferenceValue as Component;
            r.wingR = so.FindProperty("rightDetachableWing")?.objectReferenceValue as Component;
            var tails = so.FindProperty("detachableTails");
            if (tails != null && tails.isArray && tails.arraySize > 0)
                r.tail = tails.GetArrayElementAtIndex(0).objectReferenceValue as Component;
            var props = so.FindProperty("detachablePropellers");
            if (props != null && props.isArray && props.arraySize > 0)
                r.prop = props.GetArrayElementAtIndex(0).objectReferenceValue as Component;
            return r;
        }

        // ===================== HELPERS =====================
        // AddComponent helpers (no Undo: in 2022.3 Undo.AddComponent<T> sometimes returns
        // a C# valid reference before the native component is attached, causing
        // MissingComponentException when writing to a property).
        T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
        Component GetOrAdd(GameObject go, Type t)
        {
            var c = go.GetComponent(t);
            return c != null ? c : go.AddComponent(t);
        }

        Type FindTypeByName(string n)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = null;
                try { t = asm.GetType(n); } catch { }
                if (t != null) return t;
                Type[] all = null;
                try { all = asm.GetTypes(); } catch { continue; }
                foreach (var x in all) if (x.Name == n) return x;
            }
            return null;
        }

        GameObject FindChildByName(Transform parent, string name)
        {
            foreach (var t in parent.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t.gameObject;
            return null;
        }

        Component GetComponentByTypeName(Transform t, string typeName)
        {
            if (t == null) return null;
            foreach (var c in t.GetComponents<Component>())
                if (c != null && c.GetType().Name == typeName) return c;
            return null;
        }

        // ----- SerializedProperty helpers -----
        void TrySetObject(SerializedObject so, string field, UObject value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = value;
        }
        void TrySetFloat(SerializedObject so, string field, float value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.floatValue = value;
        }
        void TrySetByte(SerializedObject so, string field, byte value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.intValue = value;
        }
        void TrySetEnumInt(SerializedObject so, string field, int value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.enumValueIndex = value;
        }
        void TrySetVector3(SerializedObject so, string field, Vector3 value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.vector3Value = value;
        }
        void TrySetArrayElements(SerializedObject so, string field, UObject[] values)
        {
            var p = so.FindProperty(field);
            if (p == null || !p.isArray) return;
            p.arraySize = values?.Length ?? 0;
            if (values == null) return;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        void SetSerObj(SerializedProperty parent, string field, UObject v)
        {
            var p = parent.FindPropertyRelative(field);
            if (p != null) p.objectReferenceValue = v;
        }
        void SetSerBool(SerializedProperty parent, string field, bool v)
        {
            var p = parent.FindPropertyRelative(field);
            if (p != null) p.boolValue = v;
        }

        class PropSetter
        {
            readonly SerializedProperty p;
            public PropSetter(SerializedProperty p) { this.p = p; }
            public void SetValue(object v)
            {
                if (p == null) return;
                switch (v)
                {
                    case bool b: p.boolValue = b; break;
                    case int i: p.intValue = i; break;
                    case float f: p.floatValue = f; break;
                    case string s: p.stringValue = s; break;
                    case UObject o: p.objectReferenceValue = o; break;
                    default: break;
                }
            }
        }
        PropSetter TryGetProp(SerializedObject so, string name)
        {
            var p = so.FindProperty(name);
            return p != null ? new PropSetter(p) : null;
        }
    }
}
#endif