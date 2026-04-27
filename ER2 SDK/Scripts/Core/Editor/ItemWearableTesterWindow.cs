#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ItemWearableTesterWindow : EditorWindow
{
    private enum ItemKindFilter
    {
        All,
        Clothing,
        Helmets,
        Uniforms,
        Gear
    }

    private class Entry
    {
        public string name;
        public string path;
        public GameObject prefab;
        public ItemClothing clothing;
        public ItemHelmet helmet;

        public bool IsClothing => clothing != null;
        public bool IsHelmet => helmet != null;
    }

    private readonly List<Entry> allEntries = new List<Entry>();
    private readonly List<Entry> filteredEntries = new List<Entry>();

    private Vector2 scroll;
    private string search = "";
    private ItemKindFilter filter = ItemKindFilter.All;
    private SleeveMode sleeveMode = SleeveMode.unrolled;
    private int selectedIndex = -1;

    [MenuItem("ER2 TOOLS/Tools/Wearable Tester")]
    public static void Open()
    {
        var window = GetWindow<ItemWearableTesterWindow>("Wearable Tester");
        window.minSize = new Vector2(420, 520);
        window.Show();
    }

    private void OnGUI()
    {
        DrawToolbar();

        GUILayout.Space(6);

        DrawOptions();

        GUILayout.Space(6);

        DrawList();

        HandleKeyboard();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            RefreshList();

        GUILayout.Space(8);

        string newSearch = GUILayout.TextField(search ?? "", GUILayout.MinWidth(120));
        if (newSearch != search)
        {
            search = newSearch;
            ApplyFilter();
        }

        if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(45)))
        {
            search = "";
            GUI.FocusControl(null);
            ApplyFilter();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawOptions()
    {
        EditorGUI.BeginChangeCheck();

        filter = (ItemKindFilter)EditorGUILayout.EnumPopup("Filter", filter);
        sleeveMode = (SleeveMode)EditorGUILayout.EnumPopup("Uniform sleeve", sleeveMode);

        if (EditorGUI.EndChangeCheck())
            ApplyFilter();

        EditorGUILayout.HelpBox(
            "Click an item to select and test it. Use Up/Down arrows to move through the list. Enter tests the selected item again.",
            MessageType.Info
        );
    }

    private void DrawList()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Items: {filteredEntries.Count}", EditorStyles.boldLabel);

        GUI.enabled = selectedIndex >= 0 && selectedIndex < filteredEntries.Count;
        if (GUILayout.Button("Ping Prefab", GUILayout.Width(90)))
            PingSelected();
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < filteredEntries.Count; i++)
        {
            DrawEntry(i, filteredEntries[i]);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawEntry(int index, Entry entry)
    {
        bool selected = index == selectedIndex;

        GUIStyle style = new GUIStyle(EditorStyles.label);
        Rect rect = EditorGUILayout.BeginHorizontal();

        if (selected)
            EditorGUI.DrawRect(rect, new Color(0.24f, 0.38f, 0.60f, 0.35f));

        string icon = entry.IsHelmet ? "🪖" : entry.clothing.type == WearableType.uniform ? "👕" : "🎒";
        string typeLabel = entry.IsHelmet ? "Helmet" : entry.clothing.type.ToString();

        GUILayout.Label(icon, GUILayout.Width(24));
        GUILayout.Label(entry.name, style, GUILayout.MinWidth(160));
        GUILayout.Label(typeLabel, EditorStyles.miniLabel, GUILayout.Width(70));

        if (GUILayout.Button("Ping", GUILayout.Width(50)))
        {
            SelectIndex(index, false);
            PingSelected();
        }

        EditorGUILayout.EndHorizontal();

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            SelectIndex(index, true); // questo già testa automaticamente
            Event.current.Use();
        }
    }
    private void OnDisable()
    {
        // Quando chiudi la finestra
        if (TPSRigTesterManager.FindTester() != null)
            TPSRigTesterManager.DisableTester();
    }

    private void OnDestroy()
    {
        // Sicurezza extra (alcune versioni di Unity chiamano solo uno dei due)
        if (TPSRigTesterManager.FindTester() != null)
            TPSRigTesterManager.DisableTester();
    }
    private void HandleKeyboard()
    {
        Event e = Event.current;
        if (e == null || e.type != EventType.KeyDown)
            return;

        if (filteredEntries.Count == 0)
            return;

        if (e.keyCode == KeyCode.DownArrow)
        {
            SelectIndex(Mathf.Clamp(selectedIndex + 1, 0, filteredEntries.Count - 1), true);
            e.Use();
        }
        else if (e.keyCode == KeyCode.UpArrow)
        {
            SelectIndex(Mathf.Clamp(selectedIndex - 1, 0, filteredEntries.Count - 1), true);
            e.Use();
        }
        else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
        {
            TestSelected();
            e.Use();
        }
    }

    private void SelectIndex(int index, bool test)
    {
        if (index < 0 || index >= filteredEntries.Count)
            return;

        selectedIndex = index;

        var entry = filteredEntries[selectedIndex];
        Selection.activeObject = entry.prefab;
        EditorGUIUtility.PingObject(entry.prefab);

        if (test)
            TestEntry(entry);

        Repaint();
    }

    private void TestSelected()
    {
        if (selectedIndex < 0 || selectedIndex >= filteredEntries.Count)
            return;

        TestEntry(filteredEntries[selectedIndex]);
    }

    private void PingSelected()
    {
        if (selectedIndex < 0 || selectedIndex >= filteredEntries.Count)
            return;

        Selection.activeObject = filteredEntries[selectedIndex].prefab;
        EditorGUIUtility.PingObject(filteredEntries[selectedIndex].prefab);
    }

    private void TestEntry(Entry entry)
    {
        if (entry == null)
            return;

        if (entry.clothing != null)
        {
            if (entry.clothing.type == WearableType.uniform)
                TPSRigTesterManager.LinkUniform(entry.clothing, sleeveMode);
            else
                TPSRigTesterManager.LinkGear(entry.clothing);

            return;
        }

        if (entry.helmet != null)
            TPSRigTesterManager.LinkHelmet(entry.helmet);
    }

    private void RefreshList()
    {
        allEntries.Clear();

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            ItemClothing clothing = prefab.GetComponent<ItemClothing>();
            ItemHelmet helmet = prefab.GetComponent<ItemHelmet>();

            if (clothing == null && helmet == null)
                continue;

            allEntries.Add(new Entry
            {
                name = prefab.name,
                path = path,
                prefab = prefab,
                clothing = clothing,
                helmet = helmet
            });
        }

        allEntries.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        filteredEntries.Clear();

        string s = search == null ? "" : search.Trim().ToLowerInvariant();

        foreach (Entry entry in allEntries)
        {
            if (!PassesKindFilter(entry))
                continue;

            if (!string.IsNullOrEmpty(s))
            {
                string n = entry.name.ToLowerInvariant();
                string p = entry.path.ToLowerInvariant();

                if (!n.Contains(s) && !p.Contains(s))
                    continue;
            }

            filteredEntries.Add(entry);
        }

        if (filteredEntries.Count == 0)
            selectedIndex = -1;
        else
            selectedIndex = Mathf.Clamp(selectedIndex, 0, filteredEntries.Count - 1);

        Repaint();
    }

    private bool PassesKindFilter(Entry entry)
    {
        switch (filter)
        {
            case ItemKindFilter.Clothing:
                return entry.IsClothing;

            case ItemKindFilter.Helmets:
                return entry.IsHelmet;

            case ItemKindFilter.Uniforms:
                return entry.IsClothing && entry.clothing.type == WearableType.uniform;

            case ItemKindFilter.Gear:
                return entry.IsClothing && entry.clothing.type == WearableType.gear;

            default:
                return true;
        }
    }
}
#endif