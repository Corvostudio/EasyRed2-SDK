#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VehicleSeat))]
[CanEditMultipleObjects]
public partial class VehicleSeatEditor : Editor
{
    private const string GUIDE_URL =
        "https://wiki.easyred2.com/page.php?slug=string-tables-for-custom-properties#section-606";

    // Cache properties (so we can selectively draw/hide fields)
    private SerializedProperty _onSeatAnimatorId;
    private SerializedProperty _canCrouch;
    private SerializedProperty _manualPreviewType; // only exists in editor builds in your VehicleSeat

    private void OnEnable()
    {
        _onSeatAnimatorId = serializedObject.FindProperty("onSeatAnimator_id");
        _canCrouch = serializedObject.FindProperty("canCrouch");
        _manualPreviewType = serializedObject.FindProperty("manualPreviewType"); // may be null if you renamed it
    }

    partial void OnInspectorGUIPrivate();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // --- Draw core fields (and optionally hide manual preview when rule is found) ---

        if (_onSeatAnimatorId != null) EditorGUILayout.PropertyField(_onSeatAnimatorId);
        if (_canCrouch != null) EditorGUILayout.PropertyField(_canCrouch);

        // Determine mapping status across selection
        int total = targets != null ? targets.Length : 0;
        int mapped = 0;

        if (targets != null)
        {
            foreach (var obj in targets)
            {
                var seat = obj as VehicleSeat;
                if (!seat) continue;

                if (GUILayout.Button("Make sitted soldier " + (seat.gameObject.activeSelf ? "hidden" : "visible")))
                    seat.gameObject.SetActive(!seat.gameObject.activeSelf);

                // Uses your VehicleSeat.TryGetRule(animatorId, out rule)
                if (VehicleSeat.TryGetRule(seat.onSeatAnimator_id, out _))
                    mapped++;
            }
        }

        bool allMapped = (total > 0 && mapped == total);
        bool someMapped = (mapped > 0 && mapped < total);

        // If rule exists for all selected, hide the manual preview picker
        if (allMapped)
        {
            EditorGUILayout.HelpBox(
                "Preview is driven by Animator Id mapping (rule table). Manual preview is hidden.",
                MessageType.Info);
        }
        else
        {
            if (someMapped)
            {
                EditorGUILayout.HelpBox(
                    $"Some selected seats ({mapped}/{total}) have mapped Animator Ids.\n" +
                    "Mapped seats ignore the manual preview setting; unmapped seats use the fallback.",
                    MessageType.Warning);
            }

            // Show fallback manual preview (only if the property exists)
            if (_manualPreviewType != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(_manualPreviewType, new GUIContent("Editor Preview (Fallback)"));
            }
        }

        serializedObject.ApplyModifiedProperties();

        // --- Extra UI (warning + help) ---
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Seat Origin (Transform Position) is the reference point used by the new system:\n" +
            "� where feet touch the ground OR where the butt sits on the seat.\n" +
            "Orient the seat so +Z (forward) matches the sitting direction.",
            MessageType.Warning);

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("String table / Guide", GUILayout.Width(110)))
            {
                Application.OpenURL(GUIDE_URL);
            }
        }

        //editor part
        OnInspectorGUIPrivate();

        // Tester IK mani: chiamato qui (editor pubblico) così resta disponibile
        // anche nell'SDK modding quando la partial .Runtime non è presente.
        DrawHandsIKTester();
    }

    // Keep this minimal since your gizmo already draws direction.
    // (If you want: we can show ONLY text here too, but you already do Handles.Label in VehicleSeat.)
    private void OnSceneGUI()
    {
        // Intentionally empty / minimal.
        // Your VehicleSeat gizmo+label already does the job without extra clutter.
    }

    private void DrawHandsIKTester()
    {
        // Solo selezione singola: il multi-edit del resto dell'inspector continua a funzionare,
        // ma questo pulsante deve comparire solo con UNA seat selezionata.
        if (targets == null || targets.Length != 1)
            return;

        VehicleSeat seat = target as VehicleSeat;
        if (seat == null)
            return;

        // Il pulsante esiste solo se la seat è registrata in un Vehicle padre (a qualunque livello)
        // E quella entry ha entrambi gli IK delle mani assegnati.
        if (!SeatHasUsableIK(seat))
            return;

        EditorGUILayout.Space(10);
        GUILayout.Label("HANDS IK TESTER", EditorStyles.boldLabel);

        HandsPlacementCheckerSeat active = FindActiveTesterForSeat(seat);

        if (active == null)
        {
            if (GUILayout.Button("Spawn Hands IK Tester"))
                SpawnHandsTester(seat);
        }
        else
        {
            if (GUILayout.Button("Remove Hands IK Tester"))
                RemoveHandsTester(active);
        }

        EditorGUILayout.HelpBox(
            "Preview editor-only: istanzia il prefab esistente del placement-checker delle mani e lo aggancia " +
            "agli IK di questa seat. Si pulisce da solo in Play / cambio scena / ricompilazione.",
            MessageType.None);
    }

    private static bool SeatHasUsableIK(VehicleSeat seat)
    {
        if (seat == null)
            return false;

        Vehicle veh = seat.GetComponentInParent<Vehicle>(true);
        if (veh == null || veh.seats == null)
            return false;

        foreach (Seats s in veh.seats)
        {
            if (s != null && s.seat == seat)
                return s.lefthandIsIK != null && s.rightHandIK != null;
        }
        return false;
    }

    private static HandsPlacementCheckerSeat FindActiveTesterForSeat(VehicleSeat seat)
    {
        foreach (var c in Resources.FindObjectsOfTypeAll<HandsPlacementCheckerSeat>())
        {
            if (c == null) continue;
            if (EditorUtility.IsPersistent(c)) continue; // ignora i prefab asset
            if (c.attached_seat == seat) return c;
        }
        return null;
    }

    private void SpawnHandsTester(VehicleSeat seat)
    {
        GameObject prefab = FindHandsCheckerPrefab();
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(
                "Hands IK Tester",
                "Nessun prefab di placement-checker delle mani trovato nel progetto.\n\n" +
                "Assicurati che esista il prefab delle mani (root con un componente " +
                "HandsPlacementCheckerSteeringWheel / HandsPlacementCheckerPlaneSteer / HandsPlacementChecker " +
                "e i transform Left/Right hand assegnati).",
                "OK");
            return;
        }

        // Clone disconnesso (nessun link al prefab): posso togliere/aggiungere componenti e non viene mai salvato.
        GameObject inst = (GameObject)Object.Instantiate(prefab);
        inst.name = prefab.name + " (Seat IK Tester)";

        if (!TryGetHandRefs(inst, out Transform left, out Transform right))
        {
            Object.DestroyImmediate(inst);
            EditorUtility.DisplayDialog(
                "Hands IK Tester",
                "Il prefab trovato non ha i transform Left/Right hand assegnati sul suo componente checker.",
                "OK");
            return;
        }

        // Rimuovo i componenti checker originali così non auto-distruggono il clone.
        StripComponent<HandsPlacementCheckerSteeringWheel>(inst);
        StripComponent<HandsPlacementCheckerPlaneSteer>(inst);
        StripComponent<HandsPlacementChecker>(inst);

        var checker = inst.AddComponent<HandsPlacementCheckerSeat>();
        checker.attached_seat = seat;
        checker.left_hand = left;
        checker.right_hand = right;

        inst.transform.SetParent(null); // il checker richiede parent == null

        Undo.RegisterCreatedObjectUndo(inst, "Spawn Hands IK Tester");
        SceneView.RepaintAll();
    }

    private void RemoveHandsTester(HandsPlacementCheckerSeat tester)
    {
        if (tester == null) return;
        Undo.DestroyObjectImmediate(tester.gameObject);
        SceneView.RepaintAll();
    }

    private static void StripComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        if (c != null) Object.DestroyImmediate(c);
    }

    private static bool TryGetHandRefs(GameObject root, out Transform left, out Transform right)
    {
        left = right = null;

        var sw = root.GetComponent<HandsPlacementCheckerSteeringWheel>();
        if (sw != null) { left = sw.left_hand; right = sw.right_hand; }

        if (left == null || right == null)
        {
            var ps = root.GetComponent<HandsPlacementCheckerPlaneSteer>();
            if (ps != null) { left = ps.left_hand; right = ps.right_hand; }
        }

        if (left == null || right == null)
        {
            var hp = root.GetComponent<HandsPlacementChecker>();
            if (hp != null) { left = hp.left_hand; right = hp.right_hand; }
        }

        return left != null && right != null;
    }

    private static GameObject FindHandsCheckerPrefab()
    {
        // Restringo per nome (token) per non caricare ogni prefab, poi verifico per componente.
        string[] tokens = { "Steering", "Hand", "Placement", "Checker" };

        foreach (string token in tokens)
        {
            foreach (string guid in AssetDatabase.FindAssets($"{token} t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null) continue;

                if (go.GetComponent<HandsPlacementCheckerSteeringWheel>() != null ||
                    go.GetComponent<HandsPlacementCheckerPlaneSteer>() != null ||
                    go.GetComponent<HandsPlacementChecker>() != null)
                {
                    return go;
                }
            }
        }
        return null;
    }
}
#endif
