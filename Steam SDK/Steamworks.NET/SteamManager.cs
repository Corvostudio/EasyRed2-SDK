// SteamManager.cs - Unity 2022.3 LTS + Steamworks.NET 20.2.0+
#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif
using UnityEngine;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class SteamManager : MonoBehaviour
{
#if !DISABLESTEAMWORKS
    public const uint APP_ID = 1324780;

    private static SteamManager _instance;
    private static bool _initializationAttempted;
    private static bool _everInitialized;
    private bool _initialized;
    private SteamAPIWarningMessageHook_t _warningHook;

    public static bool Initialized => _instance != null && _instance._initialized;
    public static bool InitializationAttempted => _initializationAttempted;
    public static string LastInitError { get; private set; } = "";
    public static ESteamAPIInitResult LastInitResult { get; private set; } = ESteamAPIInitResult.k_ESteamAPIInitResult_OK;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _initializationAttempted = false;
        _everInitialized = false;
        LastInitError = "";
        LastInitResult = ESteamAPIInitResult.k_ESteamAPIInitResult_OK;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject(nameof(SteamManager));
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<SteamManager>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeSteam();
    }

    public static bool TryInitializeAgain()
    {
        if (_instance == null) Bootstrap();
        if (_instance._initialized) return true;
        Debug.Log("[Steam] Retrying SteamAPI initialization...");
        return _instance.InitializeSteam();
    }

    private bool InitializeSteam()
    {
        if (_initialized) return true;
        if (_everInitialized)
        {
            Debug.LogError("[Steam] SteamAPI was already initialized once this session.");
            return false;
        }
        _initializationAttempted = true;

        if (!Packsize.Test()) Debug.LogError("[Steam] Packsize.Test failed.");
        if (!DllCheck.Test()) Debug.LogError("[Steam] DllCheck.Test failed.");

        // RestartAppIfNecessary protetto. Se l'exe viene lanciato fuori da Steam,
        // questo lo rilancia tramite Steam. In build di release (senza steam_appid.txt
        // accanto all'exe) NON crea loop.
        try
        {
            if (SteamAPI.RestartAppIfNecessary(new AppId_t(APP_ID)))
            {
                Debug.Log("[Steam] RestartAppIfNecessary returned true. Quitting to relaunch via Steam.");
                Application.Quit();
                return false;
            }
        }
        catch (System.DllNotFoundException e)
        {
            Debug.LogError("[Steam] steam_api DLL not found.\n" + e);
            LastInitError = "steam_api DLL not found";
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Steam] Exception during RestartAppIfNecessary.\n" + e);
            // non-fatal, proseguo verso Init
        }

        // InitEx -> codice di errore esatto. Indispensabile per diagnosi (case Cina/AV).
        try
        {
            LastInitResult = SteamAPI.InitEx(out string errMsg);
            LastInitError = errMsg ?? "";
            _initialized = LastInitResult == ESteamAPIInitResult.k_ESteamAPIInitResult_OK;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Steam] Exception during SteamAPI.InitEx()\n" + e);
            LastInitError = e.Message;
            _initialized = false;
        }

        if (!_initialized)
        {
            DumpDiagnostics();
            return false;
        }
        _everInitialized = true;

        try
        {
            _warningHook = SteamAPIDebugTextHook;
            SteamClient.SetWarningMessageHook(_warningHook);
        }
        catch { }

        Debug.Log($"[Steam] SteamAPI initialized. User: {SteamFriends.GetPersonaName()} | AppID: {SteamUtils.GetAppID()}");
        return true;
    }

    private static void DumpDiagnostics()
    {
        Debug.LogError(
            $"[Steam] Init failed. Result={LastInitResult} | Msg=\"{LastInitError}\"\n" +
            $"OS: {SystemInfo.operatingSystem}\n" +
            $"Lang: {Application.systemLanguage}\n" +
            $"Path: {Application.dataPath}\n" +
            $"CWD: {System.Environment.CurrentDirectory}\n" +
            $"AppId: {APP_ID}"
        );
    }

    [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
    private static void SteamAPIDebugTextHook(int severity, System.Text.StringBuilder text)
        => Debug.LogWarning("[SteamAPI] " + text);

    private void Update()
    {
        if (_initialized) SteamAPI.RunCallbacks();
    }

    private void OnApplicationQuit() => ShutdownSteam();
    private void OnDestroy()
    {
        if (_instance == this) { ShutdownSteam(); _instance = null; }
    }

    private void ShutdownSteam()
    {
        if (!_initialized) return;
        try { SteamAPI.Shutdown(); } catch { }
        _initialized = false;
    }
#else
    public static bool Initialized => false;
    public static bool InitializationAttempted => false;
    public static string LastInitError => "";
    public static bool TryInitializeAgain() => false;
#endif
}