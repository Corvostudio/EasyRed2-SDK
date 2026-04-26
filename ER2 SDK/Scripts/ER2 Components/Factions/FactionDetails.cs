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

    //public string faction_name;
    [Header("Id name of the faction anthem audio clip")]
    public string anthem_id;
    [Header("Faction flag")]
    public Sprite flag;
    [Header("Id names of the ViceManagers created")]
    public string[] voices_id;
    
    [Header("Rank icons")]
    public Sprite[] ranks;
    [Header("Rank & pathc shoulder coords")]
    public Vector3 ranks_coords = new Vector3(.5f, 1.21f, .75f);
    public Vector3 patch_coords = new Vector3(.5f, 0.2f, .8f);

    [Header("Random names & surnames")]
    public string[] names=new string[] { "Carlo" };
    public string[] surnames = new string[] { "Rossi" };
    [Header("Faces & hair ids")]
    public string[] faces_id = new string[] { "face_caucasic_1", "face_caucasic_2", "face_caucasic_3", "face_asiatic_1", "face_asiatic_2", "face_soviet_1" };
    //public string[] hairs_id = new string[] { "Hairs 1", "Hairs 2", "Hairs 3", "Hairs 4" };


    public string ordinance_pistol_id = "colt1911";
    public string autoTransport_uniform_id = "us_police_uniform_1";
    public string autoTransport_helmet_id = "us_infantry_helmet_1";

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
