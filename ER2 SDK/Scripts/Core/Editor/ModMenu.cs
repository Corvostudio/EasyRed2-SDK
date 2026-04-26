#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using System.Xml;
using Steamworks;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using System.Reflection;
using static TextureDetails;

//NOTA
//Pare che chunk based compression in asset bundles crea asset bundles piu grandi invece che piu piccoli

public class ModMenu : EditorWindow
{
    public const string MODDING_GUIDE_URL = "https://wiki.easyred2.com/category.php?path=modding-sdk";
    public const string YT_PLAYLIST = "https://www.youtube.com/watch?v=PLHL02d4bBI&list=PLLo17ahM0Ask7n7g9uppPRPhhrRDB2UBP";

    //Main menu variables
    static List<AssetBundleBuild> found_bundles = new List<AssetBundleBuild>();
    private Vector2 scrollPos;
    int index = 0;
    static string[] bundle_names = new string[0];
    static Button editButton;
    static Texture2D coverPhoto;
    static bool canModifySWId = false;
    bool errors_found = false;

    private static bool inMainMenu = true;


    //Graphics Device variables

    UnityEngine.Rendering.GraphicsDeviceType[] SUPPORTED_GRAPHIC_API_WIN = { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11, UnityEngine.Rendering.GraphicsDeviceType.Vulkan, GraphicsDeviceType.Direct3D12 };
    UnityEngine.Rendering.GraphicsDeviceType[] SUPPORTED_GRAPHIC_API_LIN = { UnityEngine.Rendering.GraphicsDeviceType.Vulkan, UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore };
    UnityEngine.Rendering.GraphicsDeviceType[] SUPPORTED_GRAPHIC_API_MAC = { UnityEngine.Rendering.GraphicsDeviceType.Metal };



    //xml metadata variables
    private XmlParsingResult xmlData;

    //workshop data
    string ws_modChangelogs;
    ModVisibility ws_modVisibility = ModVisibility.Unlisted;


    //xml prefab editing variables
    private static string[] propTypeValues = null;




    [MenuItem("ER2 Project/Documentation/Tutorial playlist")]
    static void DocumentationTutorialPlaylist()
    {
        Application.OpenURL(YT_PLAYLIST);
    }
    [MenuItem("ER2 Project/Documentation/Guides")]
    static void DocumentationStringTables()
    {
        Application.OpenURL(MODDING_GUIDE_URL);
    }


    // Add menu named "My Window" to the Window menu
    [MenuItem("ER2 Project/MOD COMPILER")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        ModMenu window = (ModMenu)EditorWindow.GetWindow(typeof(ModMenu));
        found_bundles = RecoverBundles();
        window.Show();


        //create array of prop types
        Array array = Enum.GetValues(typeof(ModPropType));
        propTypeValues = new string[array.Length];
        for (int i = 0; i < array.Length; i++)
            propTypeValues[i] = array.GetValue(i).ToString();


        //add the build scene to the scene list
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene("Assets/ER2 SDK/Scenes/UploadScene.unity", true) };
        if (!Application.isPlaying)
            EditorSceneManager.SaveOpenScenes();// Save the changes


        //fix fog
        EnableFogStripping();
    }
    private static void EnableFogStripping()
    {
        //fog settings
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 1;
        RenderSettings.fogEndDistance = 5000;

        //fog stripping
        UnityEngine.Object graphicsSettings = GraphicsSettings.GetGraphicsSettings();
        SerializedObject serializedObject = new SerializedObject(graphicsSettings);
        serializedObject.FindProperty("m_FogStripping").intValue = 1;
        serializedObject.FindProperty("m_FogKeepLinear").boolValue = true;
        serializedObject.FindProperty("m_FogKeepExp").boolValue = true;
        serializedObject.FindProperty("m_FogKeepExp2").boolValue = true;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }


    void OnGUI()
    {
        if (inMainMenu || xmlData == null)
            PrintMainMenu();
        else
            PrintAssetsList();
    }

    private void PrintMainMenu()
    {

        //check if bundles are not found corretcly; usually happens when scripts are reloaded
        if (found_bundles == null || index >= found_bundles.Count || index < 0)
        {
            index = 0;
            Init();
        }

        //get bundle name of current selected bundle
        if (GUILayout.Button("Refresh"))
        {
            errors_found = false;
            Init();
        }

        if (found_bundles.Count < 1)
        {
            GUILayout.Label("No bundles found");
            return;
        }

        GUILayout.Label("Select AssetBundles", EditorStyles.boldLabel);
        index = EditorGUILayout.Popup(index, bundle_names);
        EditorGUILayout.Space();


        //XML INFORMATIONS
        if (index >= found_bundles.Count)
        {
            errors_found = false;
            Init();
            return;
        }


        if (xmlData == null || xmlData.bundleName != bundle_names[index])//OR IF IT's NOT THE XML OF THE BUNDLE WE ARE EDITING
        {
            errors_found = false;

            //save previous xml
            if (xmlData != null && xmlData.bundleName != bundle_names[index])
                ModUtils.SaveXmlToFile(ModUtils.CreateXmlString(xmlData), ModUtils.GetExportedModPath(xmlData.bundleName), ModUtils.XML_FILE_NAME);

            //Finalize assets assigning their ids
            if (!ModUtils.HasXML(bundle_names[index]))
                CreateNewXML(bundle_names[index]);
            else
                xmlData = ModUtils.ParseXmlString(ModUtils.GetXMLContentsFromFile(ModUtils.GetExportedModPath(bundle_names[index]), ModUtils.XML_FILE_NAME));
            coverPhoto = ModUtils.GetCoverPhotoImage(found_bundles[index].assetBundleName);
        }

        //change pic
        if (GUILayout.Button(coverPhoto, GUILayout.MaxWidth(150), GUILayout.MaxHeight(150)))
        {
            Texture2D cover = ModUtils.LoadTextureFromFile(EditorUtility.OpenFilePanel("Select cover image", null, "jpg,jpeg,png,bmp"));
            ModUtils.SaveTextureToJPG(cover, ModUtils.GetExportedModPath(xmlData.bundleName), "cover", ModUtils.COVER_RESOLUTION, ModUtils.COVER_RESOLUTION);
            coverPhoto = ModUtils.GetCoverPhotoImage(found_bundles[index].assetBundleName);
        }
        GUILayout.Space(15);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Mod Title Name");
        xmlData.modName = GUILayout.TextField(xmlData.modName);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Author Name: " + xmlData.userName.ToString());
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (!canModifySWId)
        {
            if (xmlData.workshopFileId == 0)
                GUILayout.Label("Upload on new Workshop File", GUILayout.Width(280));
            else
                GUILayout.Label("Workshop File ID: " + xmlData.workshopFileId.ToString(), GUILayout.Width(280));
        }
        else
        {
            GUILayout.Label("Set Workshop File ID (0 = new)", GUILayout.Width(280));

            ulong previous_id = xmlData.workshopFileId;
            string new_id = GUILayout.TextField(xmlData.workshopFileId.ToString());
            if (string.IsNullOrEmpty(new_id))
            {
                xmlData.workshopFileId = 0;
            }
            else
            {
                try
                { xmlData.workshopFileId = ulong.Parse(new_id); }
                catch (Exception)
                { xmlData.workshopFileId = previous_id; }
            }
        }
        canModifySWId = EditorGUILayout.ToggleLeft("Edit", canModifySWId);
        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();
        GUILayout.Label("Mod description: ");
        xmlData.modDescription = GUILayout.TextArea(xmlData.modDescription, GUILayout.MinWidth(120));
        GUILayout.EndHorizontal();


        //BUILD DATA

        //workshop changelogs
        GUILayout.Label("Changelogs (Workshop): ");
        ws_modChangelogs = GUILayout.TextArea(ws_modChangelogs, GUILayout.MinWidth(120), GUILayout.MaxHeight(80));

        EditorGUILayout.Space();

        //open eula
        if (GUILayout.Button("Open EULA"))
        {
            Application.OpenURL("https://steamcommunity.com/workshop/workshoplegalagreement/");
        }


        //check what's in the bundle so far
        if (GUILayout.Button("Check assets in the bundle"))
        {
            errors_found = false;
            if (!ScanAssetsInBundle(xmlData))
                errors_found = true;
            //PrintAssetsinXML(xmlData);
            inMainMenu = false;
        }

        EditorGUILayout.Space();


        GUILayout.Label("Workshop visibility: ");
        EditorGUILayout.BeginHorizontal();
        //workshop visibility
        Array array = Enum.GetValues(typeof(ModVisibility));
        string[] modVisibilityTitles = new string[array.Length];
        for (int i = 0; i < array.Length; i++)
            modVisibilityTitles[i] = array.GetValue(i).ToString();
        ws_modVisibility = (ModVisibility)EditorGUILayout.Popup((int)ws_modVisibility, modVisibilityTitles);

        //BUILD SELECTEDS
        if (GUILayout.Button("Build"))
        {
            Build(xmlData);
        }
        GUILayout.Label("Make sure to close games/softwares running on Steam first!");
        EditorGUILayout.EndHorizontal();

        if (errors_found)
            GUILayout.Label("Some issues have been found! (Check the console)");

    }

    private void PrintAssetsList()
    {
        //check what's in the bundle so far
        if (GUILayout.Button("Back"))
        {
            inMainMenu = true;
        }
        GUILayout.Label("Total objects found in bundle: " + xmlData.prefab_name.Count);
        GUILayout.Label("-----------------------------------------------------------------------------------------");
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < xmlData.prefab_name.Count; i++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(xmlData.prefab_name[i]);
            //GUILayout.Label(xmlData.display_name[i]);
            GUILayout.Label(xmlData.prefab_prop_type[i].ToString());
            GUILayout.EndHorizontal();
            GUILayout.Label("-----------------------------------------------------------------------------------------");
        }
        GUILayout.EndScrollView();
    }


    private static List<AssetBundleBuild> RecoverBundles()
    {
        List<AssetBundleBuild> tmp = new List<AssetBundleBuild>();
        bundle_names = AssetDatabase.GetAllAssetBundleNames();
        foreach (string bundleName in bundle_names)
        {
            string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);

            // Create an AssetBundleBuild object for the current asset bundle
            var bundleBuild = new AssetBundleBuild();
            bundleBuild.assetBundleName = bundleName;
            bundleBuild.assetNames = assetPaths;

            // Add the AssetBundleBuild object to the list
            tmp.Add(bundleBuild);
        }
        return tmp;
    }

    [Obsolete]
    private static bool ScanAssetsInBundle(XmlParsingResult xmlParsingdata)
    {
        //cleanup old data
        xmlParsingdata.Clear();

        //start scanning stuff in the bundle
        string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(xmlParsingdata.bundleName);
        foreach (string assetPath in assetPaths)
        {
            UnityEngine.Object asset_object = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            GameObject asset = (asset_object is GameObject)?(GameObject)asset_object:null;

            if (asset) //assets gameobjects
            {
                //cameras can't be in assets
                Camera cam = asset.GetComponentInChildren<Camera>();
                if (cam)
                {
                    Debug.LogError("Prefab '" + asset.name + "' contains a Camera component. Please remove it and try again.", cam.gameObject);
                    return false;
                }

                //lights shouldn't be in assets
                Light light = asset.GetComponentInChildren<Light>();
                if (light)
                {
                    Debug.LogError("Prefab '" + asset.name + "' contains a Light component. Is this intended?", light.gameObject);
                }

                //start adding to prefab map
                if (asset.GetComponent<ItemObject>())
                {
                    ForceMeshCollidersConvex(asset);
                    ItemObject io = asset.GetComponent<ItemObject>();

                    //add id to itemobject
                    string new_id = xmlParsingdata.bundleName + "_" + asset.name;
                    if (io.item_id != new_id)
                    {
                        io.item_id = new_id;//assign id to asset (bundlename_assetname)
                        EditorUtility.SetDirty(io); // Mark the asset as dirty to ensure changes are saved


                        // Save changes to the asset if it is a prefab
                        PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType(asset);
                        if (assetType == PrefabAssetType.Regular || assetType == PrefabAssetType.Variant)
                        {
                            try
                            {
                                PrefabUtility.SaveAsPrefabAsset(asset, assetPath);
                                Debug.Log("Changes saved for asset: " + asset.name);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError("Some assets cannot be built in the bundle (maybe they are not prefabs but imported resources)\n" + assetPath + "\n\n" + e, asset);
                            }
                        }
                    }

                    //reggister in xml
                    if (io is GenericGun)
                    {
                        AddItemToXml(asset, ModPropType.weapons, xmlParsingdata);

                        //fix bundle name of the weapon
                        asset.GetComponent<GenericGun>().modBundleName = xmlParsingdata.bundleName;
                        //fix asset bundle name for weapon anims
                        FinalizeWeaponAnimations(asset.GetComponent<GenericGun>(), xmlParsingdata.bundleName);
                        //Magazine
                    }
                    else if (io is Magazine)
                    {
                        AddItemToXml(asset, ModPropType.ammo, xmlParsingdata);
                    }
                    else if (io is AmmoItem)
                    {
                        /*AmmoItem ammoItem = (io as AmmoItem);
                        if (ammoItem.useCustomData)
                        {
                            //AGGIUNGI ALL'XML
                            Debug.Log("Adding to virtual xml");
                            xmlParsingdata.ammoTypes.Add(ammoItem.customBulletData);
                        }*/
                        AddItemToXml(asset, ModPropType.ammo, xmlParsingdata);
                    }
                    else if (io is Attachment)
                    {
                        AddItemToXml(asset, ModPropType.attachment, xmlParsingdata);
                        if (io is AttachmentBipod)
                        {
                            Animation an = io.GetComponent<Animation>();
                            if (an)
                                foreach (AnimationState state in an)
                                    FixAnimation(state.clip.name,WrapMode.Default, xmlParsingdata.bundleName);
                        }
                    }
                    else if (io is ItemClothing || io is ItemHelmet)
                    {
                        if (io is ItemClothing)
                        {
                            ItemClothing ic = (ItemClothing)io;
                            if (ic.mesh == null)
                            {
                                Debug.LogError(io.name + " doesn't have main TPS skinned mesh assigned.", io);
                                continue;
                            }
                            if (ic.mesh_lod == null)
                            {
                                Debug.LogError(io.name + " doesn't have LOD TPS skinned mesh assigned.", io);
                                continue;
                            }
                            if (ic.materials.Length == 0)
                            {
                                Debug.LogError(io.name + " doesn't have TPS material(s) assigned.", io);
                                continue;
                            }
                            if (ic.type==WearableType.uniform)
                            {
                                if (ic.fps_mesh == null)
                                {
                                    Debug.LogError(io.name + " doesn't have FPS mesh assigned.", io);
                                    continue;
                                }
                            }
                        }
                        AddItemToXml(asset, ModPropType.uniforms, xmlParsingdata);
                    }
                    else
                    {
                        AddItemToXml(asset, ModPropType.items, xmlParsingdata);
                    }
                }
                else if (asset.GetComponent<DestructibleManager>() || asset.GetComponent<DoorRegister>())//buildings
                {
                    if (asset.GetComponentInChildren<ColliderAlwaysActive>())
                        AddItemToXml(asset, ModPropType.props_huge, xmlParsingdata);
                    else
                        AddItemToXml(asset, ModPropType.buildings, xmlParsingdata);
                }
                else if (asset.GetComponent<Vehicle>())
                {
                    ForceMeshCollidersConvex(asset);
                    AddItemToXml(asset, ModPropType.vehicles, xmlParsingdata);
                }
                else if (asset.GetComponent<VoiceManager>())
                {
                    //voice currently is not indexable from the xml as the reference is already contained in the faction data
                    //AddItemToXml(asset, ModPropType.sounds, xmlParsingdata);
                }
                else if (asset.tag== "TerrainDetail")//terrain grass
                {
                    AddItemToXml(asset, ModPropType.terrain_details, xmlParsingdata);
                }
                else//props
                {
                    if (asset.GetComponentInChildren<ColliderAlwaysActive>())
                        AddItemToXml(asset, ModPropType.props_huge, xmlParsingdata);
                    else
                        AddItemToXml(asset, ModPropType.props, xmlParsingdata);
                }
            }
            else//non-gameobjects
            {
                if (asset_object is TerrainLayer)//terrain layers
                {
                    AddItemToXml(asset_object, ModPropType.terrain_textures, xmlParsingdata);
                }
                if (asset_object is TextureDetails)//terrain layers
                {
                    if (!((TextureDetails)asset_object).IsValid())
                        return false;
                    AddItemToXml(asset_object, GetModPropType((TextureDetails)asset_object), xmlParsingdata);
                }
                else if (asset_object is FactionDetails)//factions
                {
                    FactionDetails faction = (FactionDetails)asset_object;

                    if (faction.bundleName!= xmlParsingdata.bundleName)
                        faction.bundleName = xmlParsingdata.bundleName;

                    /*if (string.IsNullOrEmpty(faction.faction_name))
                    {
                        Debug.LogError("" + faction.name + " doesn't have a name setted! Exluding from mod.", faction);
                        continue;
                    }*/

                    //CONTROLLO ANTHEM
                    string[] guids =string.IsNullOrEmpty(faction.anthem_id)?null: AssetDatabase.FindAssets(faction.anthem_id + " t:AudioClip");
                    if (guids == null || guids.Length==0)
                    {
                        //TODO: INTERROMPERE TUTTO E FARE IN MODO DI RIPORTARE L'ERRORE SENZA BUILDARE
                        Debug.LogError("Cannot find the anthem defined in the Faction " + asset_object.name + ".", faction);
                        return false;
                    }
                    //make sure anthem is in the right asset bundle (set it automatically if not)
                    AssetImporter importer = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]));
                    if (xmlParsingdata.bundleName != importer.assetBundleName)
                    {
                        importer.assetBundleName = xmlParsingdata.bundleName;
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport();
                    }

                    AudioClip ac = (AudioClip)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]), typeof(AudioClip));
                    if (ac == null)
                    {
                        Debug.LogError("Anthem for  " + asset_object.name + " not found.", faction);
                        return false;
                    }

                    if (ac.length > 30)
                    {
                        Debug.LogError("Anthem for  " + asset_object.name + " is longer than 30 seconds.", faction);
                        return false;
                    }

                    //CONTROLLO BANDIERA
                    if (faction.flag == null)
                    {
                        Debug.LogError("Flag Sprite for  " + asset_object.name + " is missing.", faction);
                        return false;
                    }

                    //CONTROLLO RANKS
                    if (faction.ranks.Length !=12)
                    {
                        Debug.LogError("The total number of ranks of  " + asset_object.name + " is not 12.", faction);
                        return false;
                    }

                    //CONTROLLO VOCI
                    if (faction.voices_id.Length == 0)
                    {
                        Debug.LogError("No Voice acting set(s) setted up for " + asset_object.name + ".", faction);
                        return false;
                    }

                    foreach (string voice_acting_id in faction.voices_id)
                    {
                        VoiceManager vm = null;
                        string asset_path = "";

                        if (!string.IsNullOrEmpty(voice_acting_id))
                        {
                            guids = AssetDatabase.FindAssets(voice_acting_id + " t:GameObject");
                            if (guids!=null && guids.Length>0)
                            {
                                asset_path = AssetDatabase.GUIDToAssetPath(guids[0]);
                                GameObject vm_obj = (GameObject)AssetDatabase.LoadAssetAtPath(asset_path, typeof(GameObject));
                                if (vm_obj != null)
                                    vm = vm_obj.GetComponent<VoiceManager>();
                            }
                        }

                        if (vm == null)
                        {
                            Debug.LogError("No Voice acting set " + voice_acting_id + " not found.", faction);
                            return false;
                        }


                        //check if all parameters in vm class are setted and there are not null voiceclips in the arrays
                        SerializedObject serializedObject = new SerializedObject(vm);
                        SerializedProperty property = serializedObject.GetIterator();
                        while (property.NextVisible(true)) // Loop through each AudioClip array in the target script
                        {
                            if (!property.hasVisibleChildren) continue;// Skip non-serialized properties
                            if (!property.isArray) continue;
                            //Debug.Log(property.arrayElementType+" "+property.name);

                            if (property.arraySize > 0)
                            {
                                for (int i = 0; i < property.arraySize; i++)
                                {
                                    SerializedProperty element = property.GetArrayElementAtIndex(i);
                                    AudioClip actemp = element.objectReferenceValue as AudioClip;
                                    if (actemp == null)
                                    {
                                        Debug.LogError("Voice acting '" + vm.name + "' has null clips on property '" + property.displayName + "'.", vm.gameObject);
                                        return false;
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogError("Voice acting '"+ vm .name+ "' doesn't have clips for property '" + property.displayName + "'.", vm.gameObject);
                                return false;
                            }
                        }

                        //check if numbers has exacly 10 elements
                        if (vm.numbers.Length != 10)
                        {
                            Debug.LogError("Voice acting '" + vm.name + "' doesn't have exactly 10 'number voices' (Currently "+ vm.numbers.Length + " 'number voices' are set).", vm.gameObject);
                            return false;
                        }


                        //make sure vm is in the right asset bundle (set it automatically if not)
                        importer = AssetImporter.GetAtPath(asset_path);
                        if (xmlParsingdata.bundleName != importer.assetBundleName)
                        {
                            importer.assetBundleName = xmlParsingdata.bundleName;
                            EditorUtility.SetDirty(importer);
                            importer.SaveAndReimport();
                        }
                    }

                    //everything is ok
                    AddItemToXml(asset_object, ModPropType.factions, xmlParsingdata);
                }
            }
        }


        //Done
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        //EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        return true;
    }

    private static ModPropType GetModPropType(TextureDetails texDetails)
    {
        switch (texDetails.textureType)
        {
            case TextureType.LUT:
                return global::ModPropType.lut_texture;
            case TextureType.Crosshair:
                return global::ModPropType.crosshair_texture;
            default:
                Debug.LogError("Texture type not assigned correctly!", texDetails);
                return global::ModPropType.crosshair_texture;
        }
    }

    private static void PrintAssetsinXML(XmlParsingResult xmlData)
    {
        string logStr = "'" + xmlData.bundleName + "' contains " + xmlData.prefab_name.Count + " assets:\n";
        for (int i = 0; i < xmlData.prefab_name.Count; i++)
            logStr += ">" + xmlData.prefab_name[i] + " (" + xmlData.prefab_prop_type[i].ToString() + ")\n";
        Debug.Log(logStr);
    }

    private static void ForceMeshCollidersConvex(GameObject go)
    {
        bool dirty = false;

        //fix colliders
        foreach (MeshCollider mc in go.GetComponentsInChildren<MeshCollider>())
        {
            if (!mc.convex)
            {
                mc.convex = true;
                dirty = true;
            }
        }

        //set dirty
        if (dirty)
        {
            Debug.LogError("Object '" + go + "' has some Non-Convex Mesh Colliders; Setting the collider(s) as Convex. NOTICE: Non-Convex Mesh Colliders should be used only on props/buildings and only if really necessary. Consider using only Box Colliders when possible.");
            UnityEditor.EditorUtility.SetDirty(go);
        }
    }

    [Obsolete]
    private static void FinalizeWeaponAnimations(GenericGun asset, string bundleName)
    {
        FixAnimation(asset.fpsAnimations.fps_putaway, WrapMode.ClampForever, bundleName, asset,true);
        FixAnimation(asset.fpsAnimations.fps_reload_full, WrapMode.ClampForever, bundleName, asset,false);
        FixAnimation(asset.fpsAnimations.fps_reload_half, WrapMode.ClampForever, bundleName, asset, false);
        FixAnimation(asset.fpsAnimations.fps_bolt_action, WrapMode.ClampForever, bundleName, asset, false);
        FixAnimation(asset.fpsAnimations.fps_chamber_open, WrapMode.ClampForever, bundleName, asset, false);
        FixAnimation(asset.fpsAnimations.fps_chamber, WrapMode.ClampForever, bundleName, asset, false);
        FixAnimation(asset.fpsAnimations.fps_chamber_close, WrapMode.ClampForever, bundleName, asset, false);
        FixAnimation(asset.fpsAnimations.fps_chamber_close_noAmmo, WrapMode.ClampForever, bundleName, asset, false);
        FixAnimation(asset.fpsAnimations.fps_change_barrell, WrapMode.ClampForever, bundleName, asset, false);
    }

    private static void AddItemToXml(UnityEngine.Object asset, ModPropType type, XmlParsingResult xmlParsingdata)
    {
        xmlParsingdata.prefab_name.Add(asset.name);
        xmlParsingdata.display_name.Add(asset.name);
        xmlParsingdata.prefab_prop_type.Add(type);
    }

    [Obsolete]
    private static void FixAnimation(string anim, WrapMode wrapMode,string bundleName, GenericGun gg=null, bool isEquip=false)
    {
        if (string.IsNullOrEmpty(anim))
            return;

        //Riporta path di assset che sto cercando
        string[] assets = AssetDatabase.FindAssets(anim + " t:AnimationClip");

        foreach (string guid in assets)
        {
            //fix as legacy
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".fbx"))
                continue;

            AnimationClip temp = (AnimationClip)AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip));
            if (temp.wrapMode != wrapMode || !temp.legacy)
            {
                temp.wrapMode = wrapMode;
                temp.legacy = true;
                UnityEditor.EditorUtility.SetDirty(temp);
            }

            if (gg != null)
                RemoveKeyframes.RemoveSpecificKeyframes(path, gg, isEquip);

            //make sure anthem is in the right asset bundle (set it automatically if not)
            AssetImporter importer = AssetImporter.GetAtPath(path);
            if (bundleName != importer.assetBundleName)
            {
                importer.assetBundleName = bundleName;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }
    }

    private void Build(XmlParsingResult xmlParsingData)
    {

        //check xml exists for this mod
        if (!ModUtils.HasXML(xmlParsingData.bundleName))
        {
            Debug.LogError("XML Not generated!");
            return;
        }
        SetupGraphicAPI();

        //make sure XML is loaded

        //make sure steam ID is valid
        if (!IsSteamIdValid())
        {
            Debug.LogError("You need to be logged on Steam to build this mod pack");
            //return;
        }
        /*if (!IsSameAuthor()){
             GUILayout.Label("This mod pack is not your proprety and cannot be build");
             return;
         }*/

        errors_found = false;
        if (!ScanAssetsInBundle(xmlParsingData))
        {
            errors_found = true;
            return;
        }
        //EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        ModUtils.SaveXmlToFile(ModUtils.CreateXmlString(xmlData), ModUtils.GetExportedModPath(xmlParsingData.bundleName), ModUtils.XML_FILE_NAME);


        //INITIALIZE
        System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
        stopWatch.Start();
        EditorUserBuildSettings.overrideMaxTextureSize = 2048;
        string path = ModUtils.GetExportedModPath(xmlParsingData.bundleName);
        //string path = EditorUtility.OpenFolderPanel("Select Directory", "", "");
        //BUILD BUNDLES+
        List<AssetBundleBuild> bundles_to_build = new List<AssetBundleBuild> { found_bundles[index] };
        //WINDOWS
        BuildSelectedBundles(BuildTarget.StandaloneWindows64, bundles_to_build, path, xmlParsingData.bundleName);
        //LINUX
        BuildSelectedBundles(BuildTarget.StandaloneLinux64, bundles_to_build, path, xmlParsingData.bundleName);
        //MAX
        BuildSelectedBundles(BuildTarget.StandaloneOSX, bundles_to_build, path, xmlParsingData.bundleName);
        //result
        UnityEngine.Debug.Log("Asset bundle built successfully: " + found_bundles[index] + ".");


        //DONE
        UnityEngine.Debug.Log("Custom build successfully. Total build time: " + StopWatchAndGetResult(stopWatch) + "m.");
        //RESET

        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);

        //scrivi nel file il nome del bundle compilato temporaneamente per info workshop file
        string workshopInfo = ModUtils.CreateWorkshopInfoXml(xmlParsingData.bundleName, ws_modVisibility, ws_modChangelogs);
        ModUtils.SaveXmlToFile(workshopInfo, ModUtils.GetModsRootPath, "temp.xml");

        //add upload started
        if (!GameObject.FindObjectOfType<UploadStarter>())
            new GameObject("Upload Starter").AddComponent<UploadStarter>();

        //start upload in play mode
        EditorApplication.ExecuteMenuItem("Edit/Play");
    }

    private void SetupGraphicAPI()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, SUPPORTED_GRAPHIC_API_LIN);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX, SUPPORTED_GRAPHIC_API_MAC);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows, SUPPORTED_GRAPHIC_API_WIN);
    }


    /*private bool IsSameAuthor()
    {
        //FAI CONTROLLO SU XML SE MOD PACK FATTA DA STESSO AUTORE
    }*/

    private bool IsSteamIdValid()
    {
        if (xmlData.workshopFileId < 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public float StopWatchAndGetResult(System.Diagnostics.Stopwatch stopWatch)
    {
        stopWatch.Stop(); //stop watch
        return (stopWatch.ElapsedMilliseconds / 1000 / 6) / 10f;
    }

    private void BuildSelectedBundles(BuildTarget buildTarget, List<AssetBundleBuild> builds_compressed, string path, string bundleName)
    {
        path += "\\" + bundleName + "\\" + buildTarget.ToString();

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);


        //BuildPipeline.BuildAssetBundles(path, AssetBundleBuildList.compression_assetBundles, buildTarget);//build all: unused
        if (builds_compressed.Count > 0)
            BuildPipeline.BuildAssetBundles(path, builds_compressed.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression, buildTarget);
    }


    private void CreateNewXML(string bundleName)
    {
        xmlData = new XmlParsingResult();
        xmlData.bundleName = bundleName;
        ModUtils.SaveXmlToFile(ModUtils.CreateXmlString(xmlData), ModUtils.GetExportedModPath(bundleName), ModUtils.XML_FILE_NAME);
    }
}


#endif