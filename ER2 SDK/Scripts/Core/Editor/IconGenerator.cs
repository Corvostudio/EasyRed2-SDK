#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// =====================================================================================
//  These two windows replace the old Icon Generator. Both baking tools are now
//  components (PropIconBaker / PropLODBaker), so framing settings live on the prefab
//  and re-baking is a single button. The menu items just explain the workflow and let
//  you drop the right component on the current selection.
// =====================================================================================

internal static class ER2BakeDocs
{
    public static GUIStyle Body
    {
        get
        {
            if (_body == null)
                _body = new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true };
            return _body;
        }
    }
    static GUIStyle _body;

    public static void Paragraph(string richText)
    {
        GUILayout.Label(richText, Body);
        EditorGUILayout.Space(4);
    }

    /// <summary>Adds T to the selected GameObject (or just pings it if already there).</summary>
    public static void AddComponentButton<T>(string label) where T : Component
    {
        var go = Selection.activeGameObject;
        using (new EditorGUI.DisabledScope(go == null))
        {
            if (GUILayout.Button(label, GUILayout.Height(26)))
            {
                var comp = go.GetComponent<T>();
                if (comp == null) comp = Undo.AddComponent<T>(go);
                Selection.activeObject = go;
                EditorGUIUtility.PingObject(comp);
            }
        }
        if (go == null)
            EditorGUILayout.LabelField("Select a GameObject (scene or prefab) to enable.", EditorStyles.miniLabel);
    }
}

// -------------------------------------------------------------------------------------
//  ER2 TOOLS/Tools/Texture/Icon Generator
// -------------------------------------------------------------------------------------
public class IconGenerator : EditorWindow
{
    Vector2 _scroll;

    [MenuItem("ER2 TOOLS/Tools/Texture/Icon Generator")]
    public static void ShowWindow()
    {
        var w = GetWindow<IconGenerator>("Icon Generator");
        w.minSize = new Vector2(380, 320);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.Space(4);
        ER2BakeDocs.Paragraph(
            "<b>The Icon Generator is now a component: <i>Prop Icon Baker</i>.</b>");

        ER2BakeDocs.Paragraph(
            "Add it to the same GameObject that has the <b>Vehicle</b> or <b>ItemObject</b> " +
            "(Add Component ▸ ER2/Texture/Prop Icon Baker). The framing — shot size, exposure, " +
            "shift center, resolution — is saved <b>on the prefab</b>, so you tune it once and it stays put.");

        ER2BakeDocs.Paragraph(
            "<b>Update icon</b> re-shoots straight over the currently assigned sprite, in place. " +
            "Same file, same GUID, every reference stays intact — updating an icon is one click.");

        ER2BakeDocs.Paragraph(
            "<b>Save as new icon…</b> writes a fresh PNG, imports it as a Sprite and assigns it. " +
            "The save dialog opens on the <b>current icon's folder</b> when one is already assigned.");

        ER2BakeDocs.Paragraph(
            "Camera framing is chosen automatically from the target type (vehicle / weapon / item), " +
            "exactly like the old window. Best done in <b>Prefab Mode</b> or on a root-level scene instance.");

        EditorGUILayout.Space(6);
        ER2BakeDocs.AddComponentButton<PropIconBaker>("Add Prop Icon Baker to selection");

        EditorGUILayout.EndScrollView();
    }
}

// -------------------------------------------------------------------------------------
//  ER2 TOOLS/Tools/Texture/LOD Texture Generator
// -------------------------------------------------------------------------------------
public class LODTextureGenerator : EditorWindow
{
    Vector2 _scroll;

    [MenuItem("ER2 TOOLS/Tools/Texture/LOD Texture Generator")]
    public static void ShowWindow()
    {
        var w = GetWindow<LODTextureGenerator>("LOD Texture Generator");
        w.minSize = new Vector2(380, 320);
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.Space(4);
        ER2BakeDocs.Paragraph(
            "<b>The LOD texture baker is a component: <i>Prop LOD Baker</i>.</b>");

        ER2BakeDocs.Paragraph(
            "Add it to the prop root (Add Component ▸ ER2/LOD/Prop LOD Baker), then press " +
            "<b>Auto Setup (scan children)</b>: it creates one section per distinct LOD material " +
            "(<i>FarLodShader</i> / <i>CorvoTreeBillboard</i>) found in the children.");

        ER2BakeDocs.Paragraph(
            "Each section keeps its own framing (size, distance, aperture, shift, brightness, tree/top-view flags). " +
            "On a prefab variant, Auto Setup overrides only the <b>material</b> reference and leaves framing inherited.");

        ER2BakeDocs.Paragraph(
            "<b>Override texture</b> bakes over the material's <i>_BaseMap</i> PNG in place (keeps GUID/import settings). " +
            "<b>New material + texture</b> saves a new atlas, builds a LOD material and re-points the child renderers. " +
            "<b>Override ALL textures</b> re-bakes every section at once.");

        ER2BakeDocs.Paragraph(
            "Use <b>Preview last LOD</b> / <b>Reset LOD</b> to check the billboard against the full mesh in the Scene view.");

        EditorGUILayout.Space(6);
        ER2BakeDocs.AddComponentButton<PropLODBaker>("Add Prop LOD Baker to selection");

        EditorGUILayout.EndScrollView();
    }
}
#endif