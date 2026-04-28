using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class ModMenuUpdaterAutoCheck
{
    // SessionState persists across domain reloads (recompiles, play mode)
    // but is cleared when Unity closes -> effectively "once per project open".
    private const string SessionKey_Checked = "ER2SDK_UpdateChecked";
    private const string SessionKey_QualityFixed = "ER2SDK_QualityFixed";

    static ModMenuUpdaterAutoCheck()
    {
        // Defer out of the static constructor to avoid touching EditorWindow
        // APIs while Unity is still initializing.
        EditorApplication.delayCall += RunOnceOnProjectOpen;
    }

    private static void RunOnceOnProjectOpen()
    {
        if (!SessionState.GetBool(SessionKey_QualityFixed, false))
        {
            SessionState.SetBool(SessionKey_QualityFixed, true);
            AutoFixQualityLevels();
        }

        if (!SessionState.GetBool(SessionKey_Checked, false))
        {
            SessionState.SetBool(SessionKey_Checked, true);
            ModMenuUpdater.CheckForUpdatesSilent();
        }
    }

    private static void AutoFixQualityLevels()
    {
#if !PHOTON_UNITY_NETWORKING
        // 1. Copia il QualitySettings.asset di base dall'SDK (struttura: 3 livelli con nomi e impostazioni corrette)
        TryCopyQualitySettingsFile();

        // 2. Risolvi i 3 URP asset e il Forward Renderer per nome
        var high = EditorAssetFinder.Find<RenderPipelineAsset>("UniversalRP-HighQuality");
        var medium = EditorAssetFinder.Find<RenderPipelineAsset>("UniversalRP-MediumQuality");
        var low = EditorAssetFinder.Find<RenderPipelineAsset>("UniversalRP-LowQuality");
        var forwardRenderer = EditorAssetFinder.Find<ScriptableRendererData>("Forward Rendering Asset_Renderer");

        // 3. Assegna gli URP asset ai 3 livelli di Quality
        TryAssignPipelinesToQualityLevels(high, medium, low);

        // 4. Forza il Forward Renderer come default sui 3 URP asset
        TryAssignRendererToPipelines(forwardRenderer, high, medium, low);

        // 5. High Quality come livello attivo di default
        try { QualitySettings.SetQualityLevel(0, true); }
        catch (System.Exception e) { Debug.LogWarning("ER2 SDK: impossibile impostare il quality level di default: " + e.Message); }
#endif
    }

    private static void TryCopyQualitySettingsFile()
    {
        try
        {
            string sourceFolder = Application.dataPath + "/ER2 SDK/Settings";
            string targetFolder = Directory.GetParent(Application.dataPath) + "/ProjectSettings";
            const string fileName = "QualitySettings.asset";

            string sourceFilePath = Path.Combine(sourceFolder, fileName);
            string targetFilePath = Path.Combine(targetFolder, fileName);

            if (File.Exists(sourceFilePath))
                File.Copy(sourceFilePath, targetFilePath, true);
        }
        catch (System.Exception e)
        {
            Debug.LogError("ER2 SDK: errore copiando QualitySettings.asset: " + e.Message);
        }
    }

    private static void TryAssignPipelinesToQualityLevels(RenderPipelineAsset high, RenderPipelineAsset medium, RenderPipelineAsset low)
    {
        if (!high || !medium || !low)
        {
            Debug.LogError("ER2 SDK: uno o più URP asset (HighQuality/MediumQuality/LowQuality) non trovati. Nessuna assegnazione effettuata.");
            return;
        }

        try
        {
            var qsObj = Unsupported.GetSerializedAssetInterfaceSingleton("QualitySettings");
            if (!qsObj)
            {
                Debug.LogError("ER2 SDK: impossibile ottenere il singleton QualitySettings.");
                return;
            }

            var so = new SerializedObject(qsObj);
            var levels = so.FindProperty("m_QualitySettings");

            if (levels == null || levels.arraySize < 3)
            {
                Debug.LogError("ER2 SDK: QualitySettings non contiene almeno 3 livelli.");
                return;
            }

            AssignPipelineAssetToLevel(levels.GetArrayElementAtIndex(0), high);   // slot 0 = High
            AssignPipelineAssetToLevel(levels.GetArrayElementAtIndex(1), medium); // slot 1 = Medium
            AssignPipelineAssetToLevel(levels.GetArrayElementAtIndex(2), low);    // slot 2 = Low

            so.ApplyModifiedPropertiesWithoutUndo();
        }
        catch (System.Exception e)
        {
            Debug.LogError("ER2 SDK: errore assegnando URP asset ai quality level: " + e.Message);
        }
    }

    private static void AssignPipelineAssetToLevel(SerializedProperty levelProp, RenderPipelineAsset asset)
    {
        if (levelProp == null) return;

        // Unity ha cambiato nome a questa property tra versioni; proviamo entrambi.
        var pipelineProp = levelProp.FindPropertyRelative("m_RenderPipeline")
                          ?? levelProp.FindPropertyRelative("renderPipeline");

        if (pipelineProp != null)
            pipelineProp.objectReferenceValue = asset;
    }

    private static void TryAssignRendererToPipelines(ScriptableRendererData renderer, params RenderPipelineAsset[] pipelines)
    {
        if (!renderer)
        {
            Debug.LogError("ER2 SDK: Forward Rendering Asset_Renderer non trovato. Nessuna assegnazione di renderer effettuata.");
            return;
        }

        foreach (var pipeline in pipelines)
        {
            if (!pipeline) continue;

            try
            {
                var so = new SerializedObject(pipeline);
                var list = so.FindProperty("m_RendererDataList");
                var defaultIdx = so.FindProperty("m_DefaultRendererIndex");

                if (list == null)
                {
                    Debug.LogError($"ER2 SDK: m_RendererDataList non trovato su {pipeline.name}");
                    continue;
                }

                if (list.arraySize == 0) list.arraySize = 1;
                list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;

                if (defaultIdx != null) defaultIdx.intValue = 0;

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(pipeline);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ER2 SDK: errore assegnando renderer su {pipeline.name}: {e.Message}");
            }
        }

        try { AssetDatabase.SaveAssets(); }
        catch { /* ignore */ }
    }
}