using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "RoadSignAtlasDefinition",
    menuName = "Easy Red 2/Road Signs/Atlas Definition")]
public sealed class RoadSignAtlasDefinition : ScriptableObject
{
    [Min(1)] public int columns = 8;
    [Min(1)] public int rows = 2;

    [Tooltip("If enabled, slot 0 is the top-left atlas cell. Unity UVs are still bottom-left internally.")]
    public bool indexFromTopLeft = true;

    [Tooltip("Shrinks each target UV rect in normalized UV units. Example: 2px on a 2048 texture = 2f / 2048f.")]
    [Min(0f)] public float paddingUv = 0f;

    [SerializeField] private List<string> slotNames = new List<string>();

    public int Columns => Mathf.Max(1, columns);
    public int Rows => Mathf.Max(1, rows);
    public int SlotCount => Columns * Rows;

    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        paddingUv = Mathf.Max(0f, paddingUv);
        EnsureSlotCount();
    }

    public void EnsureSlotCount()
    {
        if (slotNames == null)
            slotNames = new List<string>();

        int targetCount = SlotCount;

        while (slotNames.Count < targetCount)
            slotNames.Add($"Slot {slotNames.Count}");

        while (slotNames.Count > targetCount)
            slotNames.RemoveAt(slotNames.Count - 1);
    }

    public string GetSlotName(int index)
    {
        EnsureSlotCount();

        if (index < 0 || index >= slotNames.Count)
            return "Invalid Slot";

        string value = slotNames[index];
        return string.IsNullOrWhiteSpace(value) ? $"Slot {index}" : value;
    }

    public void SetSlotName(int index, string value)
    {
        EnsureSlotCount();

        if (index < 0 || index >= slotNames.Count)
            return;

        slotNames[index] = value;
    }

    public string[] GetPopupLabels()
    {
        EnsureSlotCount();

        string[] labels = new string[slotNames.Count];

        for (int i = 0; i < labels.Length; i++)
        {
            int col = i % Columns;
            int row = i / Columns;
            labels[i] = $"{i:00}  [{col},{row}]  {GetSlotName(i)}";
        }

        return labels;
    }

    public Rect GetUvRect(int slotIndex)
    {
        EnsureSlotCount();

        slotIndex = Mathf.Clamp(slotIndex, 0, SlotCount - 1);

        int col = slotIndex % Columns;
        int row = slotIndex / Columns;

        float cellWidth = 1f / Columns;
        float cellHeight = 1f / Rows;

        float x = col * cellWidth;

        float y = indexFromTopLeft
            ? 1f - ((row + 1) * cellHeight)
            : row * cellHeight;

        Rect rect = new Rect(x, y, cellWidth, cellHeight);

        if (paddingUv > 0f)
        {
            float padX = Mathf.Min(paddingUv, rect.width * 0.49f);
            float padY = Mathf.Min(paddingUv, rect.height * 0.49f);

            rect.xMin += padX;
            rect.xMax -= padX;
            rect.yMin += padY;
            rect.yMax -= padY;
        }

        return rect;
    }
}