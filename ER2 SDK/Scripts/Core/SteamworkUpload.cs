#if UNITY_EDITOR && UNITY_STANDALONE
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Steamworks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SteamworkUpload : MonoBehaviour
{
    private const uint APP_ID = 1324780;
    private const string SESSION_FLAG = "ER2_PendingUpload";

    private static SteamworkUpload instance;
    private static CallResult<CreateItemResult_t> createWSItem_result;
    private static CallResult<SubmitItemUpdateResult_t> submitItemUpdate_result;
    private static UGCUpdateHandle_t itemUpdate_handle;
    private static bool is_uploading;

    private static string bundleName = "";
    private static string modChangelogs;
    private static ModVisibility modVisibility;

    // UI
    private Image fillImage;
    private Text loading_text;
    private Text error_text;
    private GameObject errorPanel;
    private EItemUpdateStatus prevStatus = (EItemUpdateStatus)(-1);

    /// <summary>
    /// Editor calls this before entering Play Mode to signal that an upload is pending.
    /// </summary>
    public static void RequestUpload() => SessionState.SetBool(SESSION_FLAG, true);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!SessionState.GetBool(SESSION_FLAG, false)) return;
        SessionState.SetBool(SESSION_FLAG, false);

        // steam_appid.txt MUST exist before SteamManager's Awake fires
        CreateAppIdFile();

        var go = new GameObject("ER2_SteamWorkshopUploader");
        DontDestroyOnLoad(go);
        go.AddComponent<SteamworkUpload>();
        // NOTE: do not AddComponent<SteamManager> — it auto-creates as a persistent singleton
        // when SteamManager.Initialized is accessed.
    }

    // ---------------- LIFECYCLE ----------------

    private void Awake()
    {
        instance = this;
        Application.logMessageReceived += HandleLog;
        BuildUI();

        createWSItem_result = CallResult<CreateItemResult_t>.Create(OnWorkshopFileCreated);
        submitItemUpdate_result = CallResult<SubmitItemUpdateResult_t>.Create(OnModUploadedWorkshop);
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("SteamAPI_Init() failed. Make sure Steam is running and logged in with an account that owns Easy Red 2.");
            InterruptWithError();
            return;
        }

        try { ReadBundlename(); }
        catch (Exception e)
        {
            Debug.LogError("Cannot read upload metadata: " + e.Message);
            InterruptWithError();
            return;
        }

        string tempPath = ModUtils.GetModsRootPath + "/temp.xml";
        if (File.Exists(tempPath)) File.Delete(tempPath);

        XmlParsingResult xmlData = ModUtils.ParseXmlString(
            ModUtils.GetXMLContentsFromFile(ModUtils.GetExportedModPath(bundleName), ModUtils.XML_FILE_NAME));

        if (xmlData.workshopFileId == 0)
            createWSItem_result.Set(SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeCommunity));
        else
            UpdateWorkshopFile(new PublishedFileId_t(xmlData.workshopFileId));
    }

    private void Update()
    {
        if (!is_uploading) return;

        EItemUpdateStatus status = SteamUGC.GetItemUpdateProgress(itemUpdate_handle, out ulong proc, out ulong total);
        if (status == EItemUpdateStatus.k_EItemUpdateStatusInvalid) return;

        float perc = total == 0 ? 0f : (float)proc / (float)total;
        const float maxStage = 5f;
        float stageNum = (int)status - 1;
        fillImage.fillAmount = (stageNum / maxStage) + perc * (0.9f / maxStage);

        if (status != prevStatus)
        {
            loading_text.text = $"Uploading '{bundleName}' ({StatusLabel(status)})";
            prevStatus = status;
        }
    }

    private static string StatusLabel(EItemUpdateStatus s)
    {
        switch (s)
        {
            case EItemUpdateStatus.k_EItemUpdateStatusPreparingConfig: return "preparing config";
            case EItemUpdateStatus.k_EItemUpdateStatusPreparingContent: return "preparing content";
            case EItemUpdateStatus.k_EItemUpdateStatusUploadingContent: return "uploading content";
            case EItemUpdateStatus.k_EItemUpdateStatusUploadingPreviewFile: return "uploading preview";
            case EItemUpdateStatus.k_EItemUpdateStatusCommittingChanges: return "committing";
            default: return "working";
        }
    }

    private static Sprite _whiteSprite;
    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        var tex = new Texture2D(2, 2);
        var px = new Color[4]; for (int i = 0; i < 4; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        tex.hideFlags = HideFlags.HideAndDontSave;
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
        _whiteSprite.hideFlags = HideFlags.HideAndDontSave;
        return _whiteSprite;
    }

    // ---------------- UI ----------------

    private void BuildUI()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
        }

        var canvasGo = new GameObject("UploadCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                  ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Full-screen dim
        var dim = MakeImage("Dim", canvasGo.transform, new Color(0.06f, 0.06f, 0.08f, 0.97f));
        Stretch(dim.rectTransform);

        // Centered panel
        var panel = MakeImage("Panel", canvasGo.transform, new Color(0.18f, 0.19f, 0.22f, 1f));
        var pr = panel.rectTransform;
        pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(960, 300);

        // Title
        loading_text = MakeText("Title", panel.transform, font, 30, TextAnchor.MiddleCenter, new Color(0.95f, 0.95f, 0.95f));
        var tr = loading_text.rectTransform;
        tr.anchorMin = new Vector2(0, 1); tr.anchorMax = new Vector2(1, 1); tr.pivot = new Vector2(0.5f, 1);
        tr.sizeDelta = new Vector2(-60, 70); tr.anchoredPosition = new Vector2(0, -30);
        loading_text.text = "Uploading mod to Steam Workshop...";
        loading_text.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.6f);

        // Progress bar bg
        var pbBg = MakeImage("ProgressBg", panel.transform, new Color(0.10f, 0.10f, 0.11f, 1f));
        var pbR = pbBg.rectTransform;
        pbR.anchorMin = pbR.anchorMax = pbR.pivot = new Vector2(0.5f, 0.5f);
        pbR.sizeDelta = new Vector2(860, 30);
        pbR.anchoredPosition = new Vector2(0, 10);

        // Progress fill
        fillImage = MakeImage("ProgressFill", pbBg.transform, new Color(0.78f, 0.39f, 0.27f, 1f));
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0;
        Stretch(fillImage.rectTransform);

        // Cancel button
        var cancelGo = new GameObject("CancelButton", typeof(Image), typeof(Button));
        cancelGo.transform.SetParent(panel.transform, false);
        var cimg = cancelGo.GetComponent<Image>();
        cimg.color = new Color(0.78f, 0.32f, 0.32f, 1f);
        var cbtn = cancelGo.GetComponent<Button>();
        var colors = cbtn.colors; colors.highlightedColor = new Color(0.88f, 0.42f, 0.42f, 1f);
        colors.pressedColor = new Color(0.62f, 0.22f, 0.22f, 1f); cbtn.colors = colors;
        cbtn.onClick.AddListener(CancelUpload);
        var cr = cimg.rectTransform;
        cr.anchorMin = cr.anchorMax = cr.pivot = new Vector2(1, 0);
        cr.sizeDelta = new Vector2(150, 46);
        cr.anchoredPosition = new Vector2(-20, 20);

        var cText = MakeText("Text", cancelGo.transform, font, 20, TextAnchor.MiddleCenter, Color.white);
        Stretch(cText.rectTransform);
        cText.text = "Cancel";

        // Error panel
        errorPanel = new GameObject("ErrorPanel", typeof(RectTransform));
        errorPanel.transform.SetParent(panel.transform, false);
        var er = (RectTransform)errorPanel.transform;
        er.anchorMin = new Vector2(0, 0); er.anchorMax = new Vector2(1, 0); er.pivot = new Vector2(0, 0);
        er.sizeDelta = new Vector2(-200, 90); er.anchoredPosition = new Vector2(20, 20);

        error_text = MakeText("ErrorText", errorPanel.transform, font, 14, TextAnchor.LowerLeft, new Color(1f, 0.5f, 0.5f));
        Stretch(error_text.rectTransform);
        error_text.text = "";
        errorPanel.SetActive(false);
    }

    private static Image MakeImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = GetWhiteSprite();   // <-- AGGIUNTO
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private static Text MakeText(string name, Transform parent, Font font, int size, TextAnchor align, Color color)
    {
        var go = new GameObject(name, typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font; t.fontSize = size; t.alignment = align; t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    // ---------------- LOG / ERRORS ----------------

    private void HandleLog(string msg, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception) return;
        if (errorPanel == null) return;
        if (!errorPanel.activeSelf) errorPanel.SetActive(true);

        const int MAX_LEN = 1200;
        string newText = error_text.text + "> " + msg + "\n";
        if (newText.Length > MAX_LEN) newText = newText.Substring(newText.Length - MAX_LEN);
        error_text.text = newText;
    }

    public void CancelUpload()
    {
        is_uploading = false;
        EditorApplication.isPlaying = false;
    }

    private static void InterruptWithError(string error = "")
    {
        is_uploading = false;
        if (!instance) return;
        instance.loading_text.text = string.IsNullOrEmpty(error) ? "Upload failed." : "Upload failed. " + error;
        instance.fillImage.fillAmount = 0;
    }

    // ---------------- STEAM ----------------

    private static void ReadBundlename()
    {
        string ws_xml = ModUtils.GetXMLContentsFromFile(ModUtils.GetModsRootPath, "temp.xml");
        ModUtils.ParseWorkshopInfoXml(ws_xml, out bundleName, out modVisibility, out modChangelogs);
    }

    private static void OnWorkshopFileCreated(CreateItemResult_t param, bool bIOFailure)
    {
        if (bIOFailure || param.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError("Failed to create workshop file: " + param.m_eResult);
            InterruptWithError(param.m_eResult.ToString());
            return;
        }
        Debug.Log("Created workshop file: " + param.m_nPublishedFileId);
        UpdateWorkshopFile(param.m_nPublishedFileId);
    }

    private static void UpdateWorkshopFile(PublishedFileId_t wsFileID)
    {
        XmlParsingResult xmlData = ModUtils.ParseXmlString(
            ModUtils.GetXMLContentsFromFile(ModUtils.GetExportedModPath(bundleName), ModUtils.XML_FILE_NAME));
        xmlData.userName = SteamFriends.GetPersonaName();
        xmlData.workshopFileId = (ulong)wsFileID;
        xmlData.bundleName = bundleName;
        ModUtils.SaveXmlToFile(ModUtils.CreateXmlString(xmlData),
            ModUtils.GetExportedModPath(bundleName), ModUtils.XML_FILE_NAME);

        itemUpdate_handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), wsFileID);
        SteamUGC.SetItemTitle(itemUpdate_handle, xmlData.modName);
        SteamUGC.SetItemDescription(itemUpdate_handle, xmlData.modDescription);
        SteamUGC.SetItemTags(itemUpdate_handle, GetTagsArray(xmlData));

        string coverPath = ModUtils.GetCoverPhotoPath(bundleName);
        if (File.Exists(coverPath)) SteamUGC.SetItemPreview(itemUpdate_handle, coverPath);
        else Debug.LogWarning("Cover image not found at: " + coverPath);

        SteamUGC.SetItemVisibility(itemUpdate_handle, GetVisibility());
        SteamUGC.SetItemContent(itemUpdate_handle, ModUtils.GetExportedModPath(bundleName));

        SteamAPICall_t handle = SteamUGC.SubmitItemUpdate(itemUpdate_handle, modChangelogs);
        submitItemUpdate_result.Set(handle);
        is_uploading = true;
    }

    private void OnModUploadedWorkshop(SubmitItemUpdateResult_t param, bool bIOFailure)
    {
        is_uploading = false;
        switch (param.m_eResult)
        {
            case EResult.k_EResultOK:
                Debug.Log("Upload succesful!");
                XmlParsingResult xml = ModUtils.ParseXmlString(
                    ModUtils.GetXMLContentsFromFile(ModUtils.GetExportedModPath(bundleName), ModUtils.XML_FILE_NAME));
                Application.OpenURL("https://steamcommunity.com/sharedfiles/filedetails/?id=" + xml.workshopFileId);
                StartCoroutine(WaitAndShutdown());
                break;
            case EResult.k_EResultInvalidParam:
                Debug.LogError("Failed: invalid Workshop file ID. Try resetting it to zero in MOD COMPILER.");
                InterruptWithError(); break;
            case EResult.k_EResultLimitExceeded:
                Debug.LogError("Failed: rejected by Steam Workshop. Make sure no game is running on Steam, restart Steam if needed, and that the mod is below 1Gb.");
                InterruptWithError(); break;
            case EResult.k_EResultNoConnection:
                Debug.LogError("Failed: Steam appears offline. Go online or restart Steam.");
                InterruptWithError(); break;
            case EResult.k_EResultFileNotFound:
                Debug.LogError("Failed: Wrong Workshop file ID. Set the correct ID in MOD COMPILER, or set it to 0 to upload as new.");
                InterruptWithError(); break;
            default:
                Debug.LogError("Upload failed: " + param.m_eResult);
                InterruptWithError(); break;
        }
    }

    private IEnumerator WaitAndShutdown()
    {
        fillImage.fillAmount = 1;
        loading_text.text = "Upload completed.";
        yield return new WaitForSeconds(3);
        EditorApplication.isPlaying = false;
    }

    private static ERemoteStoragePublishedFileVisibility GetVisibility()
    {
        switch (modVisibility)
        {
            case ModVisibility.FriendsOnly: return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityFriendsOnly;
            case ModVisibility.Public: return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic;
            case ModVisibility.Unlisted: return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityUnlisted;
            case ModVisibility.Private: return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate;
            default:
                InterruptWithError("File visibility error.");
                return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate;
        }
    }

    private static List<string> GetTagsArray(XmlParsingResult xmlData)
    {
        var tags = new HashSet<string>();
        foreach (ModPropType propType in xmlData.prefab_prop_type)
        {
            switch (propType)
            {
                case ModPropType.attachment:
                case ModPropType.ammo:
                case ModPropType.items:
                case ModPropType.weapons: tags.Add("Weapons & Items"); break;
                case ModPropType.buildings:
                case ModPropType.props_huge:
                case ModPropType.props: tags.Add("Props & Buildings"); break;
                case ModPropType.vehicles: tags.Add("Vehicles"); break;
                case ModPropType.sounds: tags.Add("Sounds"); break;
                case ModPropType.uniforms: tags.Add("Uniforms"); break;
                case ModPropType.factions: tags.Add("Factions"); break;
                case ModPropType.terrain_textures:
                case ModPropType.terrain_details: tags.Add("Map Making"); break;
                case ModPropType.lut_texture:
                case ModPropType.crosshair_texture: tags.Add("UI"); break;
            }
        }
        return new List<string>(tags);
    }

    private static void CreateAppIdFile()
    {
        string filePath = Directory.GetParent(Application.dataPath).FullName + "/steam_appid.txt";
        File.WriteAllText(filePath, APP_ID.ToString());
    }
}
#endif