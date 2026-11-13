using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class FactionDetails : ScriptableObject
{
    [HideInInspector]
    public string bundleName = "";

    [Header("Flag and anthems")]
    [Tooltip("Flag of the faction.")]
    public Sprite flag;
    [Tooltip("Name of the anthem audio clip: played at the end of a battle won by this faction.")]
    public string anthem_id;
    [Tooltip("Optional: clip played instead when the player wins with this faction.")]
    public string anthem_id_win;
    [Tooltip("Optional: clip played instead when the player loses with this faction.")]
    public string anthem_id_lose;

    [Header("Soldiers")]
    [Tooltip("Prefab names of the voice sets (Voice Manager) of the faction: one is picked at random.")]
    public string[] voices_id;
    [Tooltip("Rank icons, from the lowest rank to the highest.")]
    public Sprite[] ranks;
    [Tooltip("First names for the random names of the soldiers.")]
    public string[] names=new string[] { "Carlo" };
    [Tooltip("Surnames for the random names of the soldiers.")]
    public string[] surnames = new string[] { "Rossi" };
    [Tooltip("IDs of the faces the soldiers of the faction can have.")]
    public string[] faces_id = new string[] { "face_caucasic_1", "face_caucasic_2", "face_caucasic_3", "face_asiatic_1", "face_asiatic_2", "face_soviet_1" };

    [Header("Equipment")]
    [Tooltip("ID of the pistol a surrendered soldier gets when he takes up arms again.")]
    public string ordinance_pistol_id = "colt1911";
    [Tooltip("ID of the uniform of auto transport drivers.")]
    public string autoTransport_uniform_id = "us_police_uniform_1";
    [Tooltip("ID of the headgear of auto transport drivers.")]
    public string autoTransport_helmet_id = "us_infantry_helmet_1";
    [Tooltip("Optional gear/vest of auto transport drivers. Empty = none.")]
    public string autoTransport_gear_id = "";

    [HideInInspector]//deprecated: not read by the game, the uniform sets it (Item Clothing)
    public Vector3 ranks_coords = new Vector3(.5f, 1.21f, .75f);
    [HideInInspector]//deprecated: not read by the game, the uniform sets it (Item Clothing)
    public Vector3 patch_coords = new Vector3(.5f, 0.2f, .8f);

#if UNITY_EDITOR
    public void Init()
    {
        //faction_name = "My Faction";
        anthem_id = "anthem_us_example";
        flag = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/ER2 SDK/UI/flag_pc.png", typeof(Sprite));
        voices_id = new string[] {  };
        ranks = new Sprite[12];
        ranks[0]= (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Examples/Factions/Ranks/rank_pvt.png", typeof(Sprite));
        ranks[1] = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Examples/Factions/Ranks/rank_pre_corporal.png", typeof(Sprite));
        ranks[2] = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Examples/Factions/Ranks/rank_corporal.png", typeof(Sprite));
        ranks[3] = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Examples/Factions/Ranks/rank_pre_seargent.png", typeof(Sprite));
        ranks[4] = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Examples/Factions/Ranks/rank_seargent.png", typeof(Sprite));
        ranks[5] = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Examples/Factions/Ranks/rank_staff_seargent.png", typeof(Sprite));
        ranks[6] = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Examples/Factions/Ranks/rank_technical_seargent.png", typeof(Sprite));
        ranks[7] = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Examples/Factions/Ranks/rank_first_seargent.png", typeof(Sprite));
        ranks[8] = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Examples/Factions/Ranks/rank_first_seargent.png", typeof(Sprite));
        ranks[9] = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Examples/Factions/Ranks/rank_first_seargent.png", typeof(Sprite));
        ranks[10] = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Examples/Factions/Ranks/rank_first_seargent.png", typeof(Sprite));
        ranks[11] = (Sprite)AssetDatabase.LoadAssetAtPath("Assets/Examples/Factions/Ranks/rank_first_seargent.png", typeof(Sprite));

    }

    [MenuItem("Assets/ER2 TOOLS/Faction/New Faction Data", false, 2001)]
    public static void SetUpFaction()
    {
        FactionDetails sc = ScriptableObject.CreateInstance<FactionDetails>();
        sc.Init();

        string clickedAssetGuid = Selection.assetGUIDs[0];
        string path = AssetDatabase.GUIDToAssetPath(clickedAssetGuid);
        path += "/My Faction (Rename me).asset";


        UnityEditor.AssetDatabase.CreateAsset(sc, path);
    }


    [MenuItem("Assets/ER2 TOOLS/Faction/New Voice Acting Data", false, 2001)]
    public static void CreateVoiceActing()
    {
        if (Selection.activeObject && Selection.activeObject is FactionDetails)
        {
            FactionDetails fd = (FactionDetails)Selection.activeObject;
            int id = fd.voices_id.Length + 1;
            string voice_name = fd.name + " (Voice set " + id + ")";
            GameObject vs = new GameObject(voice_name);
            vs.AddComponent<VoiceManager>();

            //add voice to faction array
            List<string> old_voices = new List<string>();
            foreach (string s in fd.voices_id)
                old_voices.Add(s);
            old_voices.Add(voice_name);
            fd.voices_id = old_voices.ToArray();
        }
        else
        {
            Debug.LogError("To create a voice acting set make sure to right click on a Faction Data!");
        }
    }
#endif

}
