using System.Text;
using UnityEngine;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class VehicleDamagablePart : Interagible, ImpactSpecifier
{
    [Tooltip("Defines impact effect type")]
    public ImpactType impactType=ImpactType.metal;

    [Tooltip("Defines thickness of the armor in mm")]
    [Range(0, 250)]
    public float armorThickness = 70;//in mm

    [Tooltip("Allows angled armor to be considered as higher thickness")]
    public bool considerAngledArmor = true;
    [Tooltip("Allows bullets to ricochet")]
    public bool canRicochet = true;

    [Tooltip("Allows to damage unit when collide with something")]
    public bool damagePlayerOnCollision = true;

    [Tooltip("When something collides with this, this vehicle get damages (only for static vehicles)")]
    public bool damageHullOnGettingRammed = true;

}


#if UNITY_EDITOR
public partial class VehicleDamagablePart
{
    // ---- Global toggle (anti-clutter): Tools menu, persisted in EditorPrefs ----
    const string kLabelMenu = "Tools/Vehicle Armor/Show Thickness Labels";

    static bool LabelsEnabled
    {
        get => EditorPrefs.GetBool(kLabelMenu, true);
        set => EditorPrefs.SetBool(kLabelMenu, value);
    }

    [MenuItem(kLabelMenu)]
    static void ToggleLabels() => LabelsEnabled = !LabelsEnabled;

    [MenuItem(kLabelMenu, true)]
    static bool ToggleLabelsValidate()
    {
        Menu.SetChecked(kLabelMenu, LabelsEnabled);
        return true;
    }

    // ---- Gizmo entry points ----
    //void OnDrawGizmos() => DrawArmor(false);
    void OnDrawGizmosSelected() => DrawArmor(true);

    void DrawArmor(bool selected)
    {
        if (!Selection.gameObjects.Contains(gameObject))
            return;

        var col = GetComponent<Collider>(); // the collider that bounds this armor part
        if (col == null) return;

        // Always show a compact label when enabled; full stats only when selected.
        if (!selected && !LabelsEnabled) return;
        Handles.Label(col.bounds.center, BuildLabel(selected), GetLabelStyle());
    }

    // Gradient tied to the [Range(0,250)] slider: red (thin) -> yellow -> green (thick).
    static Color ArmorColor(float mm)
    {
        float t = Mathf.Clamp01(mm / 250f);
        return t < 0.5f
            ? Color.Lerp(new Color(0.90f, 0.20f, 0.20f), new Color(0.95f, 0.80f, 0.20f), t * 2f)
            : Color.Lerp(new Color(0.95f, 0.80f, 0.20f), new Color(0.30f, 0.85f, 0.30f), (t - 0.5f) * 2f);
    }

    string BuildLabel(bool selected)
    {
        string hex = ColorUtility.ToHtmlStringRGB(ArmorColor(armorThickness));
        var sb = new StringBuilder();
        sb.Append($"<color=#{hex}><b>{armorThickness:0} mm</b></color>");

        if (selected)
        {
            sb.Append($"\n<size=9><color=#cfcfcf>{impactType}");
            if (considerAngledArmor) sb.Append("  \u00b7 angled");
            if (canRicochet) sb.Append("  \u00b7 ricochet");
            sb.Append("</color></size>");
            // Add more stats here if needed, e.g.:
            // sb.Append($"\n<size=9><color=#9f9f9f>ram hull: {damageHullOnGettingRammed}</color></size>");
        }
        return sb.ToString();
    }

    // ---- Cached label style: dark rounded-ish pill so text stays readable on bright scenes ----
    static GUIStyle _labelStyle;
    static Texture2D _bg;

    static GUIStyle GetLabelStyle()
    {
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle
            {
                richText = true,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                padding = new RectOffset(6, 6, 3, 3)
            };
            _labelStyle.normal.textColor = Color.white;
            _labelStyle.normal.background = GetBg();
        }
        return _labelStyle;
    }

    static Texture2D GetBg()
    {
        if (_bg == null)
        {
            _bg = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            _bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
            _bg.Apply();
        }
        return _bg;
    }
}
#endif