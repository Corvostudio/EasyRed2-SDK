#if UNITY_EDITOR && UNITY_STANDALONE
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using System;
using System.IO;
using UnityEngine.UI;

public class SteamworkUpload : MonoBehaviour
{
    private static SteamworkUpload instance = null;

    const uint app_id = 1324780;
    private static CallResult<CreateItemResult_t> createWSItem_result;
    private static CallResult<SubmitItemUpdateResult_t> submitItemUpdate_result;
    private static UGCUpdateHandle_t itemUpdate_handle;
    private static bool is_uploading = false;
    
    static string bundleName = "";
    static string modChangelogs;
    static ModVisibility modVisibility;

    public GameObject uploadMenuRoot;
    public Image slider;
    public Text loading_text;

    public static string GetDocumentPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/ER2 Mods/temp.txt"; } }


    private void Awake()
    {
        instance = this;

        //inizializziamo tutte le eventuali callbacks che potremo usare
        createWSItem_result = CallResult<CreateItemResult_t>.Create(OnWorkshopFileCreated);
        submitItemUpdate_result = CallResult<SubmitItemUpdateResult_t>.Create(OnModUploadedWorkshop);
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!SteamManager.Initialized)
        {
            InterruptWithError();
            Debug.LogError("SteamAPI_Init() failed. Make sure that Steam is running and it's logged in with an account that owns Easy Red 2");
            return;
        }
        //Debug.Log("Steam Manager initialized? " + SteamManager.Initialized);

        //Recover bundle name to upload
        ReadBundlename();

        //Make sure app id file is present in project
        AppId_t id = CreateAppIdFile();
        SteamAPI.RunCallbacks();

        uploadMenuRoot.gameObject.SetActive(true);
        XmlParsingResult xmlData = ModUtils.ParseXmlString(ModUtils.GetXMLContentsFromFile(ModUtils.GetExportedModPath(bundleName), ModUtils.XML_FILE_NAME));
        if (xmlData.workshopFileId == 0)//create new file
            createWSItem_result.Set(SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeCommunity));
        else//update previous file
            UpdateWorkshopFile(new PublishedFileId_t(xmlData.workshopFileId));
    }

    private void Update()
    {
        if (is_uploading)
        {
            EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(itemUpdate_handle, out ulong punBytesProcessed, out ulong punBytesTotal);
            float percValue = punBytesTotal == 0 ? 0 : punBytesProcessed / punBytesTotal;//perc value of the current status

            float maxUpoloadStatus = 5;//max number of status
            float currentUploadStatus = (float)status-1;//current status number
            slider.fillAmount = (currentUploadStatus / maxUpoloadStatus)+ percValue*(.9f/ maxUpoloadStatus);//increase bar while status and their percentage increases
        }
    }


    private void OnModUploadedWorkshop(SubmitItemUpdateResult_t param, bool bIOFailure)
    {
        is_uploading = false;
        //Debug.Log("Upload result: "+param.m_eResult);

        switch (param.m_eResult)
        {
            case EResult.k_EResultOK:
                StartCoroutine(WaitAndShutdown());
                Debug.Log("Upload succesful!");
                //open url mod
                XmlParsingResult xmlData1 = ModUtils.ParseXmlString(ModUtils.GetXMLContentsFromFile(ModUtils.GetExportedModPath(bundleName), ModUtils.XML_FILE_NAME));
                Application.OpenURL("https://steamcommunity.com/sharedfiles/filedetails/?id="+ xmlData1.workshopFileId);
                break;

            case EResult.k_EResultInvalidParam:
                XmlParsingResult xmlData = ModUtils.ParseXmlString(ModUtils.GetXMLContentsFromFile(ModUtils.GetExportedModPath(bundleName), ModUtils.XML_FILE_NAME));
                Debug.LogError("Failed: Can't upload on workshop file ID '"+ xmlData.workshopFileId+ "'. Try to set a valid Workshop File ID in the MOD COMPILER MENU or reset it to zero.\n");
                InterruptWithError();
                break;
            case EResult.k_EResultLimitExceeded:
                Debug.LogError("Failed: Upload rejected by Steam Workshop. Make sure no game is running on steam. If this error persist try to restart Steam or make sure your mod files are not bigger than 1Gb.");
                InterruptWithError();
                break;
            case EResult.k_EResultNoConnection:
                Debug.LogError("Failed: Steam seems offline, please go online or restart Steam if the issue persist.");
                InterruptWithError();
                break;
            case EResult.k_EResultFileNotFound:
                Debug.LogError("Failed: Wrong Workshop file ID. To recover the ID number go to the mod in the workshop and retrive the number at the end of the link then copy it and go to 'mod menu'->'edit xml' and paste it in the workshop file id field, or set it to 0 if you want to upload the mod as a new workshop file.");
                InterruptWithError();
                break;
            default:
                Debug.LogError("Upload failed: " + param.m_eResult);
                InterruptWithError();
                break;
        }
    }

    private IEnumerator WaitAndShutdown()
    {
        slider.fillAmount = 1;
        loading_text.text = "Upload completed.";
        yield return new WaitForSeconds(4);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;//exit playmode
#endif
    }

    private static void InterruptWithError(string error="")
    {
        if (instance)
        {
            instance.loading_text.text = "Upload failed. " + error;
            is_uploading = false;
            instance.slider.fillAmount = 0;
        }
    }

    private void ReadBundlename()
    {
        string file_path = ModUtils.GetModsRootPath;
        string ws_xml_metadata = ModUtils.GetXMLContentsFromFile(file_path,"temp.xml");
        ModUtils.ParseWorkshopInfoXml(ws_xml_metadata, out bundleName, out modVisibility, out modChangelogs);

        //File.Delete(file_path+"/temp.xml");
    }

  
    private static void OnWorkshopFileCreated(CreateItemResult_t param, bool bIOFailure)
    {
        Debug.Log("Created workshop file: "+param.m_nPublishedFileId);
        UpdateWorkshopFile(param.m_nPublishedFileId);
    }
    private static void UpdateWorkshopFile(PublishedFileId_t wsFileID)
    {
        //Save workshop ID to XML
        XmlParsingResult xmlData = ModUtils.ParseXmlString(ModUtils.GetXMLContentsFromFile(ModUtils.GetExportedModPath(bundleName), ModUtils.XML_FILE_NAME));
        xmlData.userName = SteamFriends.GetPersonaName();
        xmlData.workshopFileId = (ulong)wsFileID;
        xmlData.bundleName = bundleName;
        ModUtils.SaveXmlToFile(ModUtils.CreateXmlString(xmlData), ModUtils.GetExportedModPath(bundleName), ModUtils.XML_FILE_NAME);

        //start item update
        itemUpdate_handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), wsFileID);

        //set updated file data
        SteamUGC.SetItemTitle(itemUpdate_handle, xmlData.modName);
        SteamUGC.SetItemDescription(itemUpdate_handle, xmlData.modDescription);
        SteamUGC.SetItemTags(itemUpdate_handle, GetTagsArray(xmlData));
        SteamUGC.SetItemPreview(itemUpdate_handle, ModUtils.GetCoverPhotoPath(bundleName));
        SteamUGC.SetItemVisibility(itemUpdate_handle, GetVisibility());
        SteamUGC.SetItemContent(itemUpdate_handle, ModUtils.GetExportedModPath(bundleName));


        //start upload
        SteamAPICall_t handle = SteamUGC.SubmitItemUpdate(itemUpdate_handle, modChangelogs);
        submitItemUpdate_result.Set(handle);
        is_uploading = true;
    }

    private static ERemoteStoragePublishedFileVisibility GetVisibility()
    {
        switch (modVisibility)
        {
            case ModVisibility.FriendsOnly:
                return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityFriendsOnly;
            case ModVisibility.Public:
                return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic;
            case ModVisibility.Unlisted:
                return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityUnlisted;
            case ModVisibility.Private:
                return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate;
            default:
                InterruptWithError("File visibility error.");
                return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate;
        }
    }

    private static List<string> GetTagsArray(XmlParsingResult xmlData)
    {
        List<string> found_tags = new List<string>();

        foreach (ModPropType propType in xmlData.prefab_prop_type)
        {
            switch (propType)
            {
                //weapons
                case ModPropType.attachment:
                case ModPropType.ammo:
                case ModPropType.items:
                case ModPropType.weapons:
                    if (!found_tags.Contains("Weapons & Items"))
                        found_tags.Add("Weapons & Items");
                    break;

                //props & buildings
                case ModPropType.buildings:
                case ModPropType.props_huge:
                case ModPropType.props:
                    if (!found_tags.Contains("Props & Buildings"))
                        found_tags.Add("Props & Buildings");
                    break;

                //vehicles
                case ModPropType.vehicles:
                    if (!found_tags.Contains("Vehicles"))
                        found_tags.Add("Vehicles");
                    break;

                //sounds
                case ModPropType.sounds:
                    if (!found_tags.Contains("Sounds"))
                        found_tags.Add("Sounds");
                    break;

                //uniforms
                case ModPropType.uniforms:
                    if (!found_tags.Contains("Uniforms"))
                        found_tags.Add("Uniforms");
                    break;

                //factions
                case ModPropType.factions:
                    if (!found_tags.Contains("Factions"))
                        found_tags.Add("Factions");
                    break;

                //factions
                case ModPropType.terrain_textures:
                case ModPropType.terrain_details:
                    if (!found_tags.Contains("Map Making"))
                        found_tags.Add("Map Making");
                    break;

                //factions
                case ModPropType.lut_texture:
                case ModPropType.crosshair_texture:
                    if (!found_tags.Contains("UI"))
                        found_tags.Add("UI");
                    break;
            }
        }

        return found_tags;
    }

    private AppId_t CreateAppIdFile()
    {
        string file_path = System.IO.Directory.GetParent(Application.dataPath).FullName + "/steam_appid.txt";
        if (!File.Exists(file_path))
        {
            File.WriteAllText(file_path, app_id.ToString());
        }
        else
        {
            File.WriteAllText(file_path, app_id.ToString());
        }
        return new AppId_t(app_id);
    }




}
#endif