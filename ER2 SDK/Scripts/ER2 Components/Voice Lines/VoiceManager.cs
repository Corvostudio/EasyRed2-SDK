using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public partial class VoiceManager : MonoBehaviour
{
    [Header("Soldier Lines")]
    [Tooltip("Soldier got injured.")]
    public AudioClip[] iVeBeenHit;
    public AudioClip[] medic;
    public AudioClip[] imReloading;
    public AudioClip[] imUnderFire;
    public AudioClip[] AAAAAH;
    public AudioClip[] scream_long;
    public AudioClip[] yes;
    public AudioClip[] yesSir;
    public AudioClip[] watchYourFire;
    public AudioClip[] enemyInfantrySpotted;
    public AudioClip[] enemyTankSpotted;
    public AudioClip[] enemyArtillerySpotted;
    public AudioClip[] enemyDown;
    public AudioClip[] granade;
    public AudioClip[] thankYou;
    public AudioClip[] coveringFire;
    public AudioClip[] imMoving;
    public AudioClip[] imCharging;
    public AudioClip[] iSurrender;

    [Header("Leader Lines")]
    public AudioClip[] imTakingTheLead;
    [Tooltip("Order to move to pointed location.")]
    public AudioClip[] moveThere;//se non ci sono nemici nell'area
    public AudioClip[] attackThere;//se ci sono nemici nell'area
    public AudioClip[] charge;//se ci sono nemici nell'area
    public AudioClip[] attackThatTank;//se è un carro
    public AudioClip[] attackThatVehicle;//se è un veicolo
    public AudioClip[] followMe;
    public AudioClip[] letsSpreadOut;
    public AudioClip[] lineFormation;
    public AudioClip[] columnFormation;
    public AudioClip[] timeToRetreat;

    [Header("Vehicle Lines")]
    public AudioClip[] getOut;
    public AudioClip[] getIn;

    [Header("Tank Lines")]
    public AudioClip[] letsMoveTank;
    public AudioClip[] fireTank;
    public AudioClip[] gunReloadedTank;
    public AudioClip[] enemyHittedTank;
    public AudioClip[] enemyDestroyedtank;
    public AudioClip[] enemyMissedTank;
    public AudioClip[] enemyNotPenetratedTank;
    public AudioClip[] gotHitTank;
    public AudioClip[] radiomanIsDead;
    public AudioClip[] gunnerIsDead;
    public AudioClip[] commanderIsDead;
    public AudioClip[] driverIsDead;
    public AudioClip[] illTakeHisSeat;
    public AudioClip[] getOutTankOnFire;
    public AudioClip[] getOutTankDestroyed;

    [Header("Radio Request")]
    [Tooltip("The audioClips of the radio numbers to give to the radio. You must add 10 ordered numbers. From 0 to 9. (MANDATORY)")]
    public AudioClip[] numbers;
    public AudioClip[] artillerySupportAt;
    public AudioClip[] tankSupportRequest;

    [Header("Radio Answer")]
    public AudioClip[] artilleryStrikeIncomingAt;
    public AudioClip[] keepYourHeadDown;
    public AudioClip[] noArtilleryAvailable;
    public AudioClip[] tankSupportIncoming;
    public AudioClip[] noTankAvailable;




#if UNITY_EDITOR
    [ContextMenu("Optimize All Voice Clips")]
    public void OptimizeAllVoiceClips()
    {
        var fields = typeof(VoiceManager).GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(AudioClip[]));

        HashSet<AudioClip> uniqueClips = new HashSet<AudioClip>();

        foreach (FieldInfo field in fields)
        {
            AudioClip[] clips = field.GetValue(this) as AudioClip[];
            if (clips == null)
                continue;

            foreach (AudioClip clip in clips)
            {
                if (clip != null)
                    uniqueClips.Add(clip);
            }
        }

        int changedCount = 0;
        int processed = 0;
        int total = uniqueClips.Count;

        // Batch tutti i reimport in un'unica passata: enorme speedup vs un SaveAndReimport per clip.
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (AudioClip clip in uniqueClips)
            {
                processed++;

                if (EditorUtility.DisplayCancelableProgressBar(
                        "Optimizing Voice Clips",
                        $"({processed}/{total}) {clip.name}",
                        total > 0 ? (float)processed / total : 0f))
                {
                    Debug.LogWarning("Voice clip optimization cancelled by user.");
                    break;
                }

                string path = AssetDatabase.GetAssetPath(clip);
                if (string.IsNullOrEmpty(path))
                    continue;

                AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer == null)
                    continue; // skip questo clip, non abortire tutto il loop

                bool changed = false;

                // Mono
                if (!importer.forceToMono)
                {
                    importer.forceToMono = true;
                    changed = true;
                }

                // Carica i dati audio su thread separato: niente stall sul main thread
                if (!importer.loadInBackground)
                {
                    importer.loadInBackground = true;
                    changed = true;
                }

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;

                if (settings.preloadAudioData)
                {
                    settings.preloadAudioData = false;
                    changed = true;
                }

                // CompressedInMemory: decodifica sull'audio thread, no spike di decompression-on-load
                if (settings.loadType != AudioClipLoadType.CompressedInMemory)
                {
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    changed = true;
                }

                if (settings.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    changed = true;
                }

                // 0.4 dà voce intellegibile senza artefatti, 0.25 era troppo aggressivo per il parlato
                float V_QLT = .35f;
                if (!Mathf.Approximately(settings.quality, V_QLT))
                {
                    settings.quality = V_QLT;
                    changed = true;
                }

                if (settings.sampleRateSetting != AudioSampleRateSetting.OverrideSampleRate)
                {
                    settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                    changed = true;
                }

                if (settings.sampleRateOverride != 44100)
                {
                    settings.sampleRateOverride = 44100;
                    changed = true;
                }

                if (changed)
                {
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                    changedCount++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"Optimized {changedCount} unique voice clips on {name}.");
    }


    /// <summary>
    /// Apre un folder picker, scansiona la cartella + sottocartelle e assegna ogni AudioClip
    /// trovato all'array il cui nome (in forma normalizzata) corrisponde al prefisso di token
    /// più lungo del nome del file. I clip già presenti vengono preservati; non vengono
    /// aggiunti duplicati. Ogni clip è assegnato a un solo array (il match migliore).
    /// </summary>
    public void AutoAssignClipsFromFolder()
    {
        string absPath = EditorUtility.OpenFolderPanel("Select voice clips folder", "Assets", "");
        if (string.IsNullOrEmpty(absPath))
            return;

        string projectAssets = Application.dataPath.Replace('\\', '/');
        string normalizedAbs = absPath.Replace('\\', '/');

        if (!normalizedAbs.StartsWith(projectAssets))
        {
            EditorUtility.DisplayDialog(
                "Invalid folder",
                "The selected folder must be inside this project's Assets directory.",
                "OK");
            return;
        }

        // Path Assets-relative: Application.dataPath finisce con "/Assets", quindi prependo "Assets".
        string relativePath = "Assets" + normalizedAbs.Substring(projectAssets.Length);

        // Tutti i field AudioClip[] del componente.
        FieldInfo[] fields = typeof(VoiceManager)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(AudioClip[]))
            .ToArray();

        // Mappa: nome-field-normalizzato -> FieldInfo. Pre-allocata.
        var fieldByNorm = new Dictionary<string, FieldInfo>(fields.Length);
        foreach (FieldInfo f in fields)
        {
            string norm = NormalizeName(f.Name);
            if (string.IsNullOrEmpty(norm))
                continue;

            if (fieldByNorm.ContainsKey(norm))
            {
                Debug.LogWarning(
                    $"Auto-assign: fields '{f.Name}' and '{fieldByNorm[norm].Name}' normalize to the same key '{norm}'. " +
                    $"'{f.Name}' will be ignored by name matching.");
                continue;
            }
            fieldByNorm[norm] = f;
        }

        // Pre-popolo le liste di output coi clip già assegnati (preservazione + dedup).
        var listByField = new Dictionary<FieldInfo, List<AudioClip>>(fields.Length);
        var setByField = new Dictionary<FieldInfo, HashSet<AudioClip>>(fields.Length);
        foreach (FieldInfo f in fields)
        {
            AudioClip[] existing = f.GetValue(this) as AudioClip[];
            int cap = (existing != null ? existing.Length : 0) + 4;
            var list = new List<AudioClip>(cap);
            var set = new HashSet<AudioClip>();

            if (existing != null)
            {
                foreach (AudioClip c in existing)
                {
                    if (c != null && set.Add(c))
                        list.Add(c);
                }
            }
            listByField[f] = list;
            setByField[f] = set;
        }

        // Trova tutti i clip nella cartella + sottocartelle.
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { relativePath });

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("No clips found",
                $"No AudioClip assets were found under:\n{relativePath}", "OK");
            return;
        }

        // Buffer riusabile per la tokenizzazione: evita allocazioni nel loop.
        List<string> tokenBuffer = new List<string>(16);
        StringBuilder sbBuffer = new StringBuilder(64);

        int matchedCount = 0;
        int newlyAdded = 0;
        int alreadyThere = 0;
        var unmatched = new List<string>();
        bool cancelled = false;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string clipPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (EditorUtility.DisplayCancelableProgressBar(
                        "Auto-Assigning Voice Clips",
                        $"({i + 1}/{guids.Length}) {Path.GetFileName(clipPath)}",
                        (float)(i + 1) / guids.Length))
                {
                    cancelled = true;
                    Debug.LogWarning("Auto-assign cancelled by user. Partial results were not applied.");
                    break;
                }

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                if (clip == null)
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(clipPath);

                tokenBuffer.Clear();
                Tokenize(fileName, tokenBuffer);

                FieldInfo match = null;
                // Iterazione dal più lungo al più corto: il match più lungo (=field più specifico) vince.
                for (int n = tokenBuffer.Count; n >= 1 && match == null; n--)
                {
                    sbBuffer.Clear();
                    for (int t = 0; t < n; t++)
                        sbBuffer.Append(tokenBuffer[t]);

                    fieldByNorm.TryGetValue(sbBuffer.ToString(), out match);
                }

                if (match == null)
                {
                    unmatched.Add(fileName);
                    continue;
                }

                matchedCount++;
                if (setByField[match].Add(clip))
                {
                    listByField[match].Add(clip);
                    newlyAdded++;
                }
                else
                {
                    alreadyThere++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (cancelled)
            return;

        // Applica i nuovi array con supporto Undo.
        Undo.RecordObject(this, "Auto-assign voice clips");
        foreach (FieldInfo f in fields)
        {
            f.SetValue(this, listByField[f].ToArray());
        }
        EditorUtility.SetDirty(this);

        // Marca la scena dirty se il componente vive in scena (non in un prefab asset).
        if (!PrefabUtility.IsPartOfPrefabAsset(this) && gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        // Report finale.
        var sb = new StringBuilder(256);
        sb.Append($"Auto-assign on '{name}': scanned {guids.Length} clip(s) under '{relativePath}'. ");
        sb.Append($"Matched: {matchedCount} (new: {newlyAdded}, already present: {alreadyThere}). ");
        sb.Append($"Unmatched: {unmatched.Count}.");
        if (unmatched.Count > 0)
        {
            sb.Append("\nUnmatched clips: ");
            int show = Mathf.Min(unmatched.Count, 25);
            for (int i = 0; i < show; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(unmatched[i]);
            }
            if (unmatched.Count > show)
                sb.Append($" (+{unmatched.Count - show} more)");
        }
        Debug.Log(sb.ToString(), this);
    }

    /// <summary>
    /// Restituisce la concatenazione lowercased di tutti i token di s.
    /// Equivalente al risultato di Tokenize seguito da concatenazione.
    /// </summary>
    private static string NormalizeName(string s)
    {
        var tokens = new List<string>(8);
        Tokenize(s, tokens);
        if (tokens.Count == 0) return string.Empty;
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < tokens.Count; i++)
            sb.Append(tokens[i]);
        return sb.ToString();
    }

    /// <summary>
    /// Tokenizza una stringa in token lowercased usando come separatori:
    ///  - qualsiasi carattere non alfanumerico (_, -, spazio, punto, ecc.)
    ///  - transizione lower -> upper (camelCase: "imReloading" -> "im","reloading")
    ///  - transizione lettera <-> cifra ("take2" -> "take","2")
    /// I run di lettere maiuscole consecutive restano un singolo token ("AAAAAH" -> "aaaaah").
    /// </summary>
    private static void Tokenize(string s, List<string> output)
    {
        if (string.IsNullOrEmpty(s)) return;

        var current = new StringBuilder(s.Length);

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];

            if (!char.IsLetterOrDigit(c))
            {
                if (current.Length > 0)
                {
                    output.Add(current.ToString().ToLowerInvariant());
                    current.Clear();
                }
                continue;
            }

            if (current.Length > 0)
            {
                char prev = current[current.Length - 1];
                bool split =
                    (char.IsLower(prev) && char.IsUpper(c)) || // camelCase: aB
                    (char.IsLetter(prev) && char.IsDigit(c)) || // letter -> digit: a1
                    (char.IsDigit(prev) && char.IsLetter(c));   // digit -> letter: 1a

                if (split)
                {
                    output.Add(current.ToString().ToLowerInvariant());
                    current.Clear();
                }
            }

            current.Append(c);
        }

        if (current.Length > 0)
            output.Add(current.ToString().ToLowerInvariant());
    }
#endif
}


#if UNITY_EDITOR
[CustomEditor(typeof(VoiceManager))]
[CanEditMultipleObjects]
public class VoiceManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8);

        if (GUILayout.Button("Optimize All Voice Clips"))
        {
            int managerCount = 0;

            foreach (Object obj in targets)
            {
                VoiceManager manager = obj as VoiceManager;
                if (manager == null)
                    continue;

                manager.OptimizeAllVoiceClips();
                managerCount++;
            }

            Debug.Log($"Processed {managerCount} VoiceManager component(s).");
        }

        // Auto-assign è single-target only: con più VoiceManager selezionati il button è nascosto.
        if (targets.Length == 1)
        {
            GUILayout.Space(4);
            if (GUILayout.Button("Auto-Assign Clips From Folder"))
            {
                ((VoiceManager)target).AutoAssignClipsFromFolder();
            }
        }
    }
}
#endif