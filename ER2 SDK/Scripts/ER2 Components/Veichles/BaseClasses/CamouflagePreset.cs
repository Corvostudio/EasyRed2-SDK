// Define a base ScriptableObject class called SpawnManagerScriptableObject
using UnityEngine;

// Use the CreateAssetMenu attribute to allow creating instances of this ScriptableObject from the Unity Editor.
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/CamouflagePreset", order = 1)]
[System.Serializable]
public partial class CamouflagePreset : ScriptableObject
{
    public string camoName;

    [Tooltip("First month the camo can appear, in the Introduction Year.")]
    public MonthName introductionMonth = MonthName.january;
    [Tooltip("First year the camo can appear (date of the mission).")]
    public int introductionYear=1900;

    [Tooltip("Last month the camo can appear, in the Deprecation Year.")]
    public MonthName deprecationMonth = MonthName.december;
    [Tooltip("Last year the camo can appear (date of the mission).")]
    public int deprecationYear = int.MaxValue;

    [Tooltip("Where the camo appears, by map coordinates. Asia: east of longitude 60. West of it, by latitude: Africa below 37.5, Mediterranean 37.5 to 45, Europe above 45.")]
    public CamoRegion region = CamoRegion.Europe | CamoRegion.Mediterranean;
    [Tooltip("No Snow: only on maps without snow. Only Snow: only on snowy maps. Allow Snow: both.")]
    public CamoSnowData snowData = CamoSnowData.NoSnow;

    [Range(0.1f, 10f)]
    [Tooltip("Spawn weight: Higher = more common. 1.0 = normal, 2.0 = twice as common, 0.5 = half as common")]
    public float spawnWeight = 1f; // ADD THIS

    [Range(0f, 10f)]
    [Tooltip("Scale of this camo relative to the vehicle's Camo Uv Scale.")]
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