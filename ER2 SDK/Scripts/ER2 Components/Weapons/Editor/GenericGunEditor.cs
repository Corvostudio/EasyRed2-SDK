#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(GenericGun), true)]//true: vale anche per i derivati (AutomaticGun, AutomaticGunWithAmmoBelt...)
public partial class GenericGunEditor : Editor
{
    public override void OnInspectorGUI()
    {

        GenericGun myScript = (GenericGun)target;

        if (!string.IsNullOrEmpty(myScript.modBundleName))
            GUILayout.Label("Mod bunlde name: " + myScript.modBundleName);

        if (GUILayout.Button("Enable / Disable hands placement checker") && !Application.isPlaying)
        {
            var existing = Resources.FindObjectsOfTypeAll<HandsPlacementChecker>();
            HandsPlacementChecker mine = null;
            foreach (var c in existing)
            {
                if (c == null || EditorUtility.IsPersistent(c)) continue;
                if (c.attached_weapon == myScript) { mine = c; continue; }
                DestroyImmediate(c.gameObject);
            }

            if (mine != null)
            {
                DestroyImmediate(mine.gameObject);
            }
            else
            {
                var checker = Instantiate(EditorAssetFinder.Find<GameObject>("HandsPlacementChecker"))
                    .GetComponent<HandsPlacementChecker>();
                checker.attached_weapon = myScript;
            }
        }

        if (GUILayout.Button("Enable / Disable animation tester") && !Application.isPlaying)
        {
            AnimationTesterTool.ToggleAnimationTester(myScript);
            return;
        }

        DrawAttachmentMagazineTesterUI(myScript);

        DrawDefaultInspector();
    }



    // Foldout state + magazine prefab cache (per editor instance).
    private bool _amTesterFoldout;
    private List<GameObject> _cachedMagPrefabs;
    private string _cachedMagSocket;

    void DrawAttachmentMagazineTesterUI(GenericGun gun)
    {
        if (gun == null || Application.isPlaying) return;

        bool hasAttachments = gun.supportedAttachments != null && gun.supportedAttachments.Length > 0;
        bool hasMagSocket = !string.IsNullOrEmpty(gun.magazineSocket) && gun.magazinePosition != null;
        if (!hasAttachments && !hasMagSocket) return;

        _amTesterFoldout = EditorGUILayout.Foldout(_amTesterFoldout, "Attachment & magazine tester", true);
        if (!_amTesterFoldout) return;

        using (new EditorGUI.IndentLevelScope())
        {
            if (hasAttachments) DrawAttachmentRows(gun);
            if (hasMagSocket) DrawMagazineRows(gun);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Attachments
    // ─────────────────────────────────────────────────────────────
    private void DrawAttachmentRows(GenericGun gun)
    {
        EditorGUILayout.LabelField("Attachments", EditorStyles.miniBoldLabel);

        for (int i = 0; i < gun.supportedAttachments.Length; i++)
        {
            var slot = gun.supportedAttachments[i];
            if (slot == null) continue;
            if (string.IsNullOrEmpty(slot.attachment_id) || slot.attachmentPos == null) continue;

            bool attached = slot.attachmentPos.childCount > 0;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(slot.attachment_id, GUILayout.MinWidth(120));
                if (GUILayout.Button(attached ? "Detach" : "Attach", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    if (attached)
                        DestroyAllChildrenWithUndo(slot.attachmentPos);
                    else
                        AttachAttachmentByItemId(slot.attachment_id, slot.attachmentPos, "Attach " + slot.attachment_id);

                    gun.TestSightPosition();
                    EditorUtility.SetDirty(gun);
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Magazines
    // ─────────────────────────────────────────────────────────────
    private void DrawMagazineRows(GenericGun gun)
    {
        // Build/refresh the prefab cache when the socket changes or it's never been built.
        if (_cachedMagPrefabs == null || _cachedMagSocket != gun.magazineSocket)
        {
            _cachedMagPrefabs = FindMagazinePrefabsForSocket(gun.magazineSocket);
            _cachedMagSocket = gun.magazineSocket;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Magazine (socket: " + gun.magazineSocket + ")", EditorStyles.miniBoldLabel);
            // Tiny refresh button — invalidates the cache so newly-added prefabs show up.
            if (GUILayout.Button("↻", EditorStyles.miniButton, GUILayout.Width(22)))
            {
                _cachedMagPrefabs = null;
                return;
            }
        }

        if (_cachedMagPrefabs == null || _cachedMagPrefabs.Count == 0)
        {
            EditorGUILayout.LabelField("No compatible magazine prefabs found.", EditorStyles.miniLabel);
            return;
        }

        // Resolve which prefab the currently installed magazine came from (if any).
        var installed = gun.installedMagazine;
        GameObject installedSource = installed != null
            ? PrefabUtility.GetCorrespondingObjectFromSource(installed)
            : null;

        for (int i = 0; i < _cachedMagPrefabs.Count; i++)
        {
            var prefab = _cachedMagPrefabs[i];
            if (prefab == null) continue;

            bool isThisInstalled = installedSource == prefab;

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(prefab.name, GUILayout.MinWidth(120));
                if (GUILayout.Button(isThisInstalled ? "Detach" : "Attach", EditorStyles.miniButton, GUILayout.Width(60)))
                {
                    // Always clear first — only one mag at a time fits in magazinePosition.
                    DestroyAllChildrenWithUndo(gun.magazinePosition);
                    if (!isThisInstalled)
                        AttachPrefabInstance(prefab, gun.magazinePosition, "Attach magazine " + prefab.name);
                }
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────
    private static void DestroyAllChildrenWithUndo(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(t.GetChild(i).gameObject);
    }

    private static GameObject AttachPrefabInstance(GameObject prefab, Transform parent, string undoLabel)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(inst, undoLabel);
        inst.transform.SetParent(parent, false);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;
        return inst;
    }

    private static void AttachAttachmentByItemId(string item_id, Transform parent, string undoLabel)
    {
        if (string.IsNullOrEmpty(item_id)) return;

        var guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var att = prefab.GetComponent<Attachment>();
            if (att == null || att.item_id != item_id) continue;

            AttachPrefabInstance(prefab, parent, undoLabel);
            return;
        }
        Debug.LogWarning("[Attachment Tester] No prefab with Attachment.item_id == '" + item_id + "' found in project.");
    }

    private static List<GameObject> FindMagazinePrefabsForSocket(string socket)
    {
        var results = new List<GameObject>();
        if (string.IsNullOrEmpty(socket)) return results;

        var guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            var mag = prefab.GetComponent<Magazine>();
            if (mag == null) continue;
            if (mag.socket != socket) continue;
            results.Add(prefab);
        }
        return results;
    }
}
#endif
