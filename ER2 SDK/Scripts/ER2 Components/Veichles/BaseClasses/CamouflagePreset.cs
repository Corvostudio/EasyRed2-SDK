// Define a base ScriptableObject class called SpawnManagerScriptableObject
using UnityEngine;

// Use the CreateAssetMenu attribute to allow creating instances of this ScriptableObject from the Unity Editor.
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/CamouflagePreset", order = 1)]
[System.Serializable]
public partial class CamouflagePreset : ScriptableObject
{
    public string camoName;

    public MonthName introductionMonth = MonthName.january;
    public int introductionYear=1900;

    public MonthName deprecationMonth = MonthName.december;
    public int deprecationYear = int.MaxValue;

    public CamoRegion region = CamoRegion.Europe | CamoRegion.Mediterranean;
    public CamoSnowData snowData = CamoSnowData.NoSnow;

    [Range(0.1f, 10f)]
    [Tooltip("Spawn weight: Higher = more common. 1.0 = normal, 2.0 = twice as common, 0.5 = half as common")]
    public float spawnWeight = 1f; // ADD THIS

    [Range(0f, 10f)]
    [Tooltip("Relative scale compared to the global uv scale.")]
    public float relativeCamoUvScale = 1;

    public string faction_id = "";//set a faction to force randomization on a specific country

    [System.Flags]
    [System.Serializable]
    public enum CamoRegion
    {
        None = 0,
        Africa = 1 << 0,
        Mediterranean = 1 << 1,
        Europe = 1 << 2,
        Asia = 1 << 3
    }
    [System.Serializable]
    public enum CamoSnowData
    {
        NoSnow = 0,
        AllowSnow = 10,
        OnlySnow = 20,
    }

}


public enum MonthName
{
    january = 1,
    february = 2,
    march = 3,
    april = 4,
    may = 5,
    june = 6,
    july = 7,
    august = 8,
    september = 9,
    october = 10,
    november = 11,
    december = 12
}