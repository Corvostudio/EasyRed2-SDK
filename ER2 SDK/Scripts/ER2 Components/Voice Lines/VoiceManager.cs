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
    public AudioClip[] moveThere;
    public AudioClip[] attackThere;
    public AudioClip[] charge;
    public AudioClip[] attackThatTank;
    public AudioClip[] attackThatVehicle;
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
                    continue;

                bool changed = false;

                if (!importer.forceToMono)
                {
                    importer.forceToMono = true;
                    changed = true;
                }

                if (!importer.loadInBackground)
                {
                    importer.loadInBackground = true;
                    changed = true;
                }

                AudioImporterSampleSettings settings = importer.defaultSampleSettings;

#if UNITY_2022_3_OR_NEWER
                if (settings.preloadAudioData)
                {
                    settings.preloadAudioData = false;
                    changed = true;
                }
#endif

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
    /// trovato al campo corrispondente, usando un dizionario di alias e ricerca della
    /// sottosequenza contigua di token più lunga. Clip già presenti vengono preservati;
    /// niente duplicati. Il campo 'numbers' usa logica posizionale (10 slot 0-9).
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

        string relativePath = "Assets" + normalizedAbs.Substring(projectAssets.Length);

        FieldInfo[] fields = typeof(VoiceManager)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(AudioClip[]))
            .ToArray();

        var fieldsByName = fields.ToDictionary(f => f.Name);

        // Costruisce mappa alias -> FieldInfo. Avvisa se un alias punta a un field inesistente.
        Dictionary<string, string> aliasToFieldName = BuildAliasMap();
        var aliasToField = new Dictionary<string, FieldInfo>(aliasToFieldName.Count);
        foreach (var kv in aliasToFieldName)
        {
            if (fieldsByName.TryGetValue(kv.Value, out FieldInfo fi))
                aliasToField[kv.Key] = fi;
            else
                Debug.LogWarning($"VoiceManager auto-assign: alias map references unknown field '{kv.Value}'.");
        }

        FieldInfo numbersField;
        fieldsByName.TryGetValue("numbers", out numbersField);

        // Pre-popola le liste output coi clip esistenti (preservazione + dedup). Salta 'numbers'.
        var listByField = new Dictionary<FieldInfo, List<AudioClip>>(fields.Length);
        var setByField = new Dictionary<FieldInfo, HashSet<AudioClip>>(fields.Length);
        foreach (FieldInfo f in fields)
        {
            if (numbersField != null && f == numbersField) continue;

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

        // Numbers: array fisso di 10 slot, pre-popolato dai valori esistenti.
        AudioClip[] numbersArr = new AudioClip[10];
        if (numbersField != null)
        {
            AudioClip[] existingNumbers = numbersField.GetValue(this) as AudioClip[];
            if (existingNumbers != null)
            {
                int len = Mathf.Min(existingNumbers.Length, 10);
                for (int i = 0; i < len; i++) numbersArr[i] = existingNumbers[i];
            }
        }

        // === FUZZY FALLBACK INFRASTRUCTURE ===
        // Costruisco una volta sola, fuori dal loop file:
        //  - expansion: dizionario token -> set di sinonimi (es. "hit" -> {"hurt","wounded",...})
        //  - keywordSets: per ogni field, set di parole chiave (token del nome del field
        //    espansi con sinonimi). Es. "enemyHittedTank" -> {"enemy","hitted","hit","hurt",
        //    "wounded","tank","vehicle"}.
        //  - idf: peso per ogni token = log2(N_fields/df). Token comuni (es. "tank" appare
        //    in 12 field) hanno peso basso; token rari hanno peso alto.
        var expansion = BuildSynonymExpansion();
        var keywordSets = BuildKeywordSets(fields, expansion);
        var idf = BuildIdf(keywordSets);

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { relativePath });

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("No clips found",
                $"No AudioClip assets were found under:\n{relativePath}", "OK");
            return;
        }

        // Buffer riusabili.
        List<string> tokenBuffer = new List<string>(16);
        StringBuilder sbBuffer = new StringBuilder(64);

        int matchedCount = 0;
        int newlyAdded = 0;
        int alreadyThere = 0;
        int numberAssigned = 0;
        int fuzzyMatched = 0;
        var unmatched = new List<string>();
        var numberOverlaps = new List<string>();
        // Mappa: nome field -> lista filename appena assegnati (per log per-field).
        var assignmentsByField = new Dictionary<string, List<string>>(fields.Length);
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

                // STEP 1: numbers ha priorità e logica dedicata.
                if (numbersField != null && TryDetectNumber(tokenBuffer, out int digit))
                {
                    matchedCount++;
                    if (numbersArr[digit] == null)
                    {
                        numbersArr[digit] = clip;
                        newlyAdded++;
                        numberAssigned++;
                        if (!assignmentsByField.TryGetValue("numbers", out var lst))
                        {
                            lst = new List<string>();
                            assignmentsByField["numbers"] = lst;
                        }
                        lst.Add($"{fileName}->[{digit}]");
                    }
                    else if (numbersArr[digit] == clip)
                    {
                        alreadyThere++;
                    }
                    else
                    {
                        // Slot già occupato da clip diversa: non sovrascrivere, segnala.
                        numberOverlaps.Add($"{fileName} (digit {digit} already '{numbersArr[digit].name}')");
                    }
                    continue;
                }

                // STEP 2: strip dei counter finali (cifre pure, lettere singole come 1A/2B).
                int trimEnd = tokenBuffer.Count;
                while (trimEnd > 0 && IsCounterToken(tokenBuffer[trimEnd - 1]))
                    trimEnd--;

                if (trimEnd == 0)
                {
                    unmatched.Add(fileName);
                    continue;
                }

                // STEP 3: cerca la sottosequenza CONTIGUA più lunga che matcha un alias.
                // Iterazione lunghezza-decrescente: il primo match trovato è il più lungo,
                // quindi il più specifico (es. "tankenemyhit" batte "enemyhit" che batte "hit").
                FieldInfo bestField = null;
                for (int len = trimEnd; len > 0 && bestField == null; len--)
                {
                    for (int start = 0; start + len <= trimEnd; start++)
                    {
                        sbBuffer.Clear();
                        for (int t = 0; t < len; t++)
                            sbBuffer.Append(tokenBuffer[start + t]);

                        if (aliasToField.TryGetValue(sbBuffer.ToString(), out FieldInfo fi))
                        {
                            bestField = fi;
                            break;
                        }
                    }
                }

                if (bestField == null)
                {
                    // STEP 4: fuzzy fallback. La alias map non ha trovato match esatto;
                    // provo a indovinare il field calcolando un punteggio basato su:
                    //  - sovrapposizione di token tra filename e nome del field
                    //  - sinonimi a livello parola (hit~hurt, destroy~kill, ...)
                    //  - peso IDF (token comuni come "tank" valgono meno di token rari).
                    // Soglie conservative per evitare falsi positivi.
                    int sigLen = trimEnd;
                    var fnSigTokens = new List<string>(sigLen);
                    for (int t = 0; t < sigLen; t++) fnSigTokens.Add(tokenBuffer[t]);

                    var (fuzzyField, fuzzyScore, fuzzySecond) = FuzzyMatchField(
                        fnSigTokens, keywordSets, idf, expansion);

                    const float MIN_SCORE = 0.55f;     // accetta solo match abbastanza forti
                    const float MIN_GAP = 0.12f;       // e con margine sul secondo classificato

                    if (fuzzyField != null
                        && fuzzyScore >= MIN_SCORE
                        && (fuzzyScore - fuzzySecond) >= MIN_GAP)
                    {
                        matchedCount++;
                        fuzzyMatched++;
                        if (setByField[fuzzyField].Add(clip))
                        {
                            listByField[fuzzyField].Add(clip);
                            newlyAdded++;
                            if (!assignmentsByField.TryGetValue(fuzzyField.Name, out var lst))
                            {
                                lst = new List<string>();
                                assignmentsByField[fuzzyField.Name] = lst;
                            }
                            // Marker "~" indica match fuzzy con score, così riconoscibile nel log.
                            lst.Add($"{fileName}~{fuzzyScore:F2}");
                        }
                        else
                        {
                            alreadyThere++;
                        }
                        continue;
                    }

                    // STEP 5 (fallback numbers): se nessun alias né fuzzy match, ma il filename
                    // ha una cifra singola in coda (es. "CAD_1_5"), trattalo come numero
                    // radio. Sicuro perché lo strip dei counter ha già escluso "01"/"02"
                    // (multi-cifra). Non può essere un counter di clip multi-take perché
                    // i counter usano sempre 2+ cifre (es. "_001", "_02", "_3A").
                    if (numbersField != null && tokenBuffer.Count > 0)
                    {
                        string lastTok = tokenBuffer[tokenBuffer.Count - 1];
                        if (lastTok.Length == 1 && lastTok[0] >= '0' && lastTok[0] <= '9')
                        {
                            int fallbackDigit = lastTok[0] - '0';
                            matchedCount++;
                            if (numbersArr[fallbackDigit] == null)
                            {
                                numbersArr[fallbackDigit] = clip;
                                newlyAdded++;
                                numberAssigned++;
                                if (!assignmentsByField.TryGetValue("numbers", out var lst))
                                {
                                    lst = new List<string>();
                                    assignmentsByField["numbers"] = lst;
                                }
                                lst.Add($"{fileName}->[{fallbackDigit}]");
                            }
                            else if (numbersArr[fallbackDigit] == clip)
                            {
                                alreadyThere++;
                            }
                            else
                            {
                                numberOverlaps.Add($"{fileName} (digit {fallbackDigit} already '{numbersArr[fallbackDigit].name}')");
                            }
                            continue;
                        }
                    }

                    unmatched.Add(fileName);
                    continue;
                }

                matchedCount++;
                if (setByField[bestField].Add(clip))
                {
                    listByField[bestField].Add(clip);
                    newlyAdded++;
                    if (!assignmentsByField.TryGetValue(bestField.Name, out var lst))
                    {
                        lst = new List<string>();
                        assignmentsByField[bestField.Name] = lst;
                    }
                    lst.Add(fileName);
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

        Undo.RecordObject(this, "Auto-assign voice clips");
        foreach (FieldInfo f in fields)
        {
            if (numbersField != null && f == numbersField)
            {
                f.SetValue(this, numbersArr);
                continue;
            }
            if (listByField.TryGetValue(f, out var list))
                f.SetValue(this, list.ToArray());
        }
        EditorUtility.SetDirty(this);

        if (!PrefabUtility.IsPartOfPrefabAsset(this) && gameObject.scene.IsValid())
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        // Report finale.
        var sb = new StringBuilder(512);
        sb.Append($"Auto-assign on '{name}': scanned {guids.Length} clip(s) under '{relativePath}'. ");
        sb.Append($"Matched: {matchedCount} (new: {newlyAdded}, already present: {alreadyThere}, numbers: {numberAssigned}, fuzzy: {fuzzyMatched}). ");
        sb.Append($"Unmatched: {unmatched.Count}.");

        if (numbersField != null)
        {
            var missingDigits = new List<int>();
            for (int d = 0; d < 10; d++) if (numbersArr[d] == null) missingDigits.Add(d);
            if (missingDigits.Count > 0)
                sb.Append($"\n[Numbers] Missing digits: {string.Join(",", missingDigits)}.");
        }

        if (numberOverlaps.Count > 0)
        {
            sb.Append("\n[Numbers] Skipped overlaps (slot already filled): ");
            int show = Mathf.Min(numberOverlaps.Count, 10);
            for (int i = 0; i < show; i++)
            {
                if (i > 0) sb.Append("; ");
                sb.Append(numberOverlaps[i]);
            }
            if (numberOverlaps.Count > show)
                sb.Append($" (+{numberOverlaps.Count - show} more)");
        }

        if (unmatched.Count > 0)
        {
            sb.Append("\nUnmatched clips: ");
            int show = Mathf.Min(unmatched.Count, 30);
            for (int i = 0; i < show; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(unmatched[i]);
            }
            if (unmatched.Count > show)
                sb.Append($" (+{unmatched.Count - show} more)");
        }

        // Sezione assignments per-field: ti permette di verificare a colpo d'occhio
        // se qualche clip è finito in un campo sbagliato. Se vedi un mismatch,
        // aggiungi/correggi un alias in BuildAliasMap e rilancia.
        if (assignmentsByField.Count > 0)
        {
            sb.Append("\n--- Assignments by field ---");
            // Ordino alfabeticamente per leggibilità.
            foreach (var kv in assignmentsByField.OrderBy(k => k.Key))
            {
                sb.Append($"\n  {kv.Key} ({kv.Value.Count}): ");
                int show = Mathf.Min(kv.Value.Count, 8);
                for (int i = 0; i < show; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(kv.Value[i]);
                }
                if (kv.Value.Count > show)
                    sb.Append($" (+{kv.Value.Count - show} more)");
            }
        }

        Debug.Log(sb.ToString(), this);
    }

    /// <summary>
    /// Mappa alias-normalizzato -> nome field. Ogni field ha sinonimi/varianti/typo noti.
    /// La lookup usa concatenazione lowercase (vedi NormalizeAlias / Tokenize).
    /// La sottosequenza contigua di TOKEN più lunga vince, quindi alias specifici
    /// (es. "tanktargethit", 3 token) prevalgono su generici (es. "hit", 1 token).
    /// </summary>
    private static Dictionary<string, string> BuildAliasMap()
    {
        var dict = new Dictionary<string, string>(256);

        void Add(string field, params string[] aliases)
        {
            foreach (var a in aliases)
            {
                string key = NormalizeAlias(a);
                if (string.IsNullOrEmpty(key)) continue;
                if (!dict.ContainsKey(key))
                    dict[key] = field;
                else if (dict[key] != field)
                    Debug.LogWarning(
                        $"VoiceManager alias collision on '{key}': '{dict[key]}' vs '{field}'. Keeping '{dict[key]}'.");
            }
        }

        // ---------------------------------------------------------------
        //  REGOLA D'ORO: la sottosequenza CONTIGUA di token più lunga vince.
        //  Quindi alias multi-token (es. "ourtankhit", 3 tok) batte alias
        //  generici (es. "hit", 1 tok) — non c'è bisogno di rimuovere i
        //  generici, basta avere quelli specifici. Tutti gli alias sono
        //  comparati case-insensitive e senza separatori (vedi NormalizeAlias).
        // ---------------------------------------------------------------

        // ---- Soldier lines ----
        Add("iVeBeenHit",
            "iVeBeenHit", "ivebeenhit", "imhurt", "gothurt", "gothur", "imhit",
            "beenhit", "gothit", "hurt", "hit", "imbeenhit");
        Add("medic",
            "medic");
        Add("imReloading",
            "imreloading", "imrealoading", "reloading", "reload");
        Add("imUnderFire",
            "imunderfire", "underfire", "gotsuppressed", "imsuppressed",
            "suppressed", "takingfire", "undersuppression");
        Add("AAAAAH",
            "aaaaah", "scream", "yell", "shortscream", "shout");
        Add("scream_long",
            "screamlong", "longscream", "death", "flamedeath", "gotincapacitated",
            "incapacitated", "incapacitation", "dying", "deathscream");
        Add("yes",
            "yes", "roger", "rogerthat", "affirmativeinformal", "affirmativehindi",
            "ok", "oksy", "okay", "affirmative", "copy");
        Add("yesSir",
            "yessir", "affirmativeformal", "yescommander", "yescaptain");
        Add("watchYourFire",
            "watchyourfire", "friendlyfire", "checkyourfire", "ceasefire");
        Add("enemyInfantrySpotted",
            "enemyinfantryspotted", "spotinfantry", "infantryspotted",
            "spotinfantryformal", "enemyspotted", "infantry", "spotted",
            "contactinfantry", "infantrycontact");
        Add("enemyTankSpotted",
            "enemytankspotted", "spottank", "tankspotted", "spottankformal",
            "enemytank", "contacttank", "tankcontact");
        Add("enemyArtillerySpotted",
            "enemyartilleryspotted", "spotartillery", "artilleryspotted",
            "enemyartillery", "enemyartilleryincoming", "artilleryincomingspotted");
        Add("enemyDown",
            "enemydown", "enemykilled", "kill", "killed", "niceshot",
            "gotone", "targetdown", "gothim");
        // NOTA: VehDestroyEnemy/VehKillEnemy (kill annunciato dal carrista)
        // va su enemyDestroyedtank, NON enemyDown. Vedi più sotto.
        Add("granade",
            "granade", "grenade", "throwgrenade", "smokegrenade",
            "throwingrenade", "smokegrenadethrow");
        Add("thankYou",
            "thankyou", "thanks", "gotresuscitated", "resuscitated",
            "resuscitation", "appreciated");
        Add("coveringFire",
            "coveringfire", "coverfire", "covering", "imcovering",
            "imcoveringalt", "givecoverfire");
        Add("imMoving",
            "immoving", "moving", "move", "gomove", "marching", "march",
            "advancing", "advance", "letsgo", "onthemove");
        Add("imCharging",
            "imcharging", "charging", "chargein");
        Add("iSurrender",
            "isurrender", "surrender", "imsurrendering", "imsurrenderingformal",
            "surrendering", "givingup", "ihandover");

        // ---- Leader lines ----
        Add("imTakingTheLead",
            "imtakingthelead", "takingthelead", "takinglead", "takelead",
            "takingtheleadformal", "iamtakingthelead");
        Add("moveThere",
            "movethere", "movingthere", "gothere", "movetothere",
            "moveupthere", "movetothatpoint");
        Add("attackThere",
            "attackthere", "attackpoint", "engagethere", "attackatthere");
        Add("charge",
            "charge", "chargethere", "chargethem");
        Add("attackThatTank",
            "attackthattank", "attacktank", "tankattack", "engagetank", "killthattank");
        Add("attackThatVehicle",
            "attackthatvehicle", "attackvehicle", "engagevehicle",
            "killthatvehicle", "vehicleattack");
        Add("followMe",
            "followme", "follow", "onmysix", "stickwithme");
        Add("letsSpreadOut",
            "letsspreadout", "spreadout", "disperse", "spread", "scatter");
        Add("lineFormation",
            "lineformation", "lineformal", "formline", "getbacktoline",
            "line", "formupline", "intoline");
        Add("columnFormation",
            "columnformation", "columnformal", "column", "formcolumn", "intocolumn");
        Add("timeToRetreat",
            "timetoretreat", "retreat", "orderretreat", "fallback",
            "retreatnow", "pullback", "withdraw");

        // ---- Vehicle lines ----
        Add("getOut",
            "getout", "getoutteam", "evacuate", "bailout", "outofvehicle", "vehiclegetout");
        Add("getIn",
            "getin", "boardup", "intothevehicle", "mountup", "vehiclegetin");

        // ---- Tank lines ----
        // Per ogni field tank, aggiungo SIA gli alias "tank*" SIA gli alias "veh*"
        // (alcuni voice pack usano la convenzione "Veh" come abbreviazione di "Vehicle/Tank").
        Add("letsMoveTank",
            "tankletsmove", "letsmovetank", "letsmove", "tankmove", "movetank",
            "vehmove");
        Add("fireTank",
            "tankfire", "firetank", "fireatwill", "fire", "openfire", "tankopenfire",
            "vehfire");
        Add("gunReloadedTank",
            "tankgunreloaded", "gunreloadedtank", "tankgunready",
            "gunreadytank", "gunready", "gunreloaded", "tankready", "loaded",
            "vehreload", "tankreload");
        Add("enemyHittedTank",
            "tankenemyhit", "enemyhittedtank", "tanktargethit",
            "targethittank", "enemyhit", "targethit", "wehittarget", "hittarget",
            "tankhit", "vehhitenemy", "hitenemy");
        // NOTA su "tankhit": ambiguo con "ourtankhit" (gotHitTank).
        // Risolto dalla regola "alias più lungo vince": "ourtankhit" (3 token)
        // batte "tankhit" (2 token) per i filename tipo "OurTankHit".

        Add("enemyDestroyedtank",
            "tankenemydestroyed", "enemydestroyedtank", "tanktargetdestroyed",
            "targetdestroyedtank", "enemydestroyed", "targetdestroyed",
            "tanktargetdown",
            "vehdestroyenemy", "destroyenemy", "vehkillenemy");
        Add("enemyMissedTank",
            "tankenemymissed", "enemymissedtank", "tanktargetmissed",
            "targetmissedtank", "enemymissed", "targetmissed", "missed", "miss",
            "vehmissenemy", "missenemy");
        Add("enemyNotPenetratedTank",
            "tankenemynotpenetrated", "enemynotpenetratedtank", "tanknotpenetrated",
            "notpenetratedtank", "enemynotpenetrated", "notpenetrated",
            "noeffect", "ricochet", "bouncedoff",
            "vehunpenenemy", "unpenenemy", "unpen");
        Add("gotHitTank",
            "tankgothit", "gothittank", "tankwegothit", "wegothit",
            "ourtankhit", "weretakinghits", "ourtankgothit");
        Add("radiomanIsDead",
            "radiomanisdead", "tankradiomandead", "radiomandead",
            "ourradiomandead", "radiomankilled",
            "oursignallerdead", "signallerdead", "signaller");
        Add("gunnerIsDead",
            "gunnerisdead", "tankgunnerdead", "gunnerdead",
            "ourgunnerdead", "gunnerkilled");
        Add("commanderIsDead",
            "commanderisdead", "tankcommanderdead", "commanderdead",
            "ourcommanddead", "ourcommanderdead", "commanderkilled");
        Add("driverIsDead",
            "driverisdead", "tankdriverdead", "driverdead",
            "ourdriverdead", "ourdiverdead", "driverkilled");
        Add("illTakeHisSeat",
            "illtakehisseat", "tanktakinghisseat", "takinghisseat",
            "iltaketheseat", "illtaketheseat", "takehisseat",
            "imtakingoverposition", "tanktakehisseat",
            "replaceseat");
        Add("getOutTankOnFire",
            "getouttankonfire", "tankonfire", "onfiregetout", "ourtankonfire",
            "tankonfiregetout", "tankonfiregetou", "onfire", "tanksonfire");
        Add("getOutTankDestroyed",
            "getouttankdestroyed", "tankdestroyed", "tankdestroyedgetout",
            "destroyedgetout", "ourtankdestroyed", "tankisdestroyed");

        // ---- Radio request ----
        // Includo abbreviazioni "arty" comuni in alcuni voice pack (es. cad1 - Sgoti).
        Add("artillerySupportAt",
            "artillerysupportat", "artilleryat", "requestartillery", "reqartillery",
            "reqartilleryformal", "requestartillerysupport", "radiorequestartillerysupport",
            "radiorequestartilleryat", "radioreqartillery", "radioartilleryat",
            "requestartilleryat", "callartillery", "needartillery",
            "requestarty", "reqarty", "artyat");
        Add("tankSupportRequest",
            "tanksupportrequest", "requesttank", "reqtank", "reqtanksupport",
            "requesttanksupport", "radiorequesttank", "radiorequesttanksupport",
            "radiotanksupport", "needtank", "calltanksupport");

        // ---- Radio answer ----
        Add("artilleryStrikeIncomingAt",
            "artillerystrikeincomingat", "artilleryincoming", "artilleryaccepted",
            "respondeartilleryincoming", "confirmartillery", "confirmartilleryadd",
            "radiorespondeartilleryincoming", "radioconfirmartillery",
            "artilleryincomingat", "radioartilleryincoming", "artilleryonway",
            "artilleryconfirmed",
            "confirmarty", "confirmartyadd", "artyincoming", "artyaccepted");
        Add("keepYourHeadDown",
            "keepyourheaddown", "headdown", "keepheaddown", "duckdown", "staylow");
        Add("noArtilleryAvailable",
            "noartilleryavailable", "artilleryrefused", "artillerynotavailable",
            "respondeartillerynotavailable", "denyartillery",
            "radiorespondeartillerynotavailable", "radiodenyartillery",
            "artillerydenied", "noartillery",
            "denyarty", "artyrefused", "artynotavailable", "noarty");
        Add("tankSupportIncoming",
            "tanksupportincoming", "tankincoming", "respondetankincoming",
            "confirmtank", "tankaccepted", "radiotankincoming", "radioconfirmtank",
            "radiorespondetankincoming", "tankonway", "tankconfirmed");
        Add("noTankAvailable",
            "notankavailable", "tanknotavailable", "respondetanknotavailable",
            "denytank", "tankrefused", "radiotanknotavailable", "radiodenytank",
            "radiorespondetanknotavailable", "tankdenied", "notank");

        return dict;
    }

    private static string NormalizeAlias(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }

    /// <summary>
    /// True se il token è un "counter" da strippare: cifre pure (001, 1, 23) o
    /// singola lettera (A, B come in JP_4_Move_There_1A).
    /// </summary>
    private static bool IsCounterToken(string t)
    {
        if (string.IsNullOrEmpty(t)) return false;

        bool allDigits = true;
        for (int i = 0; i < t.Length; i++)
        {
            if (!char.IsDigit(t[i])) { allDigits = false; break; }
        }
        if (allDigits) return true;

        if (t.Length == 1 && char.IsLetter(t[0])) return true;
        return false;
    }

    /// <summary>
    /// Rileva se il filename è un numero radio. Cerca un token che inizia con "num"
    /// (matcha num/number/numerical/numbers) e prende la prima cifra singola che lo
    /// segue. Le cifre PRIMA del marcatore vengono ignorate (es. "eng3_radio_num_7" -> 7).
    /// Una cifra alla fine va bene anche se attaccata al marcatore (es. "JP_4_Numerical_5"
    /// tokenizza ["jp","4","numerical","5"] e ritorna 5).
    /// </summary>
    private static bool TryDetectNumber(List<string> tokens, out int digit)
    {
        digit = -1;
        if (tokens == null || tokens.Count == 0) return false;

        int markerIdx = -1;
        for (int i = 0; i < tokens.Count; i++)
        {
            string t = tokens[i];
            if (t.Length >= 3 && t[0] == 'n' && t[1] == 'u' && t[2] == 'm')
            {
                markerIdx = i;
                break;
            }
        }

        if (markerIdx < 0) return false;

        // Cerca una cifra singola dopo il marker.
        for (int i = markerIdx + 1; i < tokens.Count; i++)
        {
            string t = tokens[i];
            if (t.Length == 1 && t[0] >= '0' && t[0] <= '9')
            {
                digit = t[0] - '0';
                return true;
            }
            // Se trovo un token non-cifra dopo il marker, continuo a cercare
            // (può esserci es. "radio_num_label_3" — improbabile ma safe).
        }

        return false;
    }

    // ============================================================
    //                  FUZZY MATCHING (STEP 4)
    // ============================================================
    //
    // Quando la alias map non ha un match esatto, il fuzzy fallback
    // entra in gioco. Funziona in 4 strati:
    //
    //  1. SynonymGroups: gruppi di parole considerate sinonimi tra
    //     loro (es. {"hit","hurt","wounded"}). Il filename token
    //     "gothurt" può così matchare un field che contiene "hit".
    //
    //  2. KeywordSets: per ogni field, l'insieme dei token derivati
    //     dal nome del field espanso con i sinonimi. Es.:
    //         enemyHittedTank -> {enemy, hitted, hit, hurt, wounded,
    //                              injured, struck, tank, vehicle}
    //
    //  3. IDF (Inverse Document Frequency): peso di ogni token =
    //     log2(N_fields / df). Token comuni come "tank" (presente in
    //     ~12 field) hanno peso basso; token rari come "destroy" hanno
    //     peso alto. Questo evita che filename con "tank" matchino tutti
    //     i field tank con score uguale.
    //
    //  4. Score = (somma IDF dei token filename che matchano il field)
    //              / (somma IDF totale dei token filename significativi).
    //     I sinonimi pesano 0.7x rispetto ai match diretti. Un match è
    //     accettato se score >= 0.55 E margine sul secondo >= 0.12.
    //
    // Le soglie sono volutamente conservative: meglio lasciare unmatched
    // un clip ambiguo (così lo vedi nel log e aggiungi alias) che
    // assegnarlo male in silenzio.

    /// <summary>
    /// Gruppi di sinonimi a livello token. Tutti i token nello stesso gruppo
    /// sono considerati equivalenti durante il fuzzy match.
    /// Da estendere quando si incontrano nuove convenzioni di naming.
    /// </summary>
    private static readonly string[][] SynonymGroups = new[]
    {
        new[] {"hit", "hurt", "wounded", "injured", "struck", "hitted"},
        new[] {"destroy", "destroyed", "kill", "killed", "down", "dead"},
        new[] {"yes", "roger", "affirmative", "ok", "okay", "copy", "oksy"},
        new[] {"move", "moving", "advance", "advancing", "march", "marching", "go"},
        new[] {"surrender", "surrendering"},
        new[] {"reload", "reloading", "reloaded", "realoading"},
        new[] {"suppressed", "suppression", "underfire"},
        new[] {"spotted", "contact", "spot", "sighted"},
        new[] {"infantry", "soldier", "soldiers"},
        new[] {"artillery", "arty"},
        new[] {"covering", "cover"},
        new[] {"charge", "charging"},
        new[] {"retreat", "fallback", "withdraw", "pullback"},
        new[] {"thanks", "thank", "appreciated", "resuscitated", "resuscitation", "thankyou"},
        new[] {"granade", "grenade"},
        new[] {"signaller", "radio", "radioman"},
        new[] {"missed", "miss"},
        new[] {"unpen", "unpenetrated", "notpenetrated", "ricochet", "bouncedoff"},
        new[] {"penetrated", "penetration"},
        new[] {"incapacitated", "incapacitation", "dying"},
        new[] {"death", "scream", "deathscream"},
        new[] {"replace", "taking"},     // ReplaceSeat ↔ TakingHisSeat
        new[] {"deny", "refuse", "refused", "denied", "notavailable"},
        new[] {"confirm", "accept", "accepted", "incoming", "onway"},
        new[] {"request", "req", "call", "need"},
        new[] {"tank", "vehicle"},
        new[] {"veh", "vehicle"},
        new[] {"watch", "check"},
        new[] {"line", "lineformation"},
        new[] {"column", "columnformation"},
        new[] {"spread", "spreadout", "disperse", "scatter"},
        new[] {"lead", "leader"},
        new[] {"head"},                  // keepHeadDown
        new[] {"smoke"},                 // smokeGrenade
        new[] {"throw"},                 // throwGrenade
        new[] {"flame"},                 // flameDeath
        new[] {"friendly"},              // friendlyFire
        new[] {"helmet"},                // CH_4_Helmet (no field) — neutral
    };

    /// <summary>
    /// Token troppo generici per contribuire al match. NOTA BENE: "our",
    /// "enemy", "got" NON sono qui perché disambiguano OurTankHit vs
    /// EnemyTankHit (chi spara vs chi viene colpito).
    /// </summary>
    private static readonly HashSet<string> Stopwords = new HashSet<string>
    {
        "i", "im", "ive", "the", "a", "an", "of", "to", "at", "in", "on",
        "and", "or", "is", "be", "that", "this", "as", "by", "with", "for",
        "lets", "let", "now", "alt", "formal", "informal", "hindi"
    };

    private static Dictionary<string, HashSet<string>> BuildSynonymExpansion()
    {
        var d = new Dictionary<string, HashSet<string>>();
        foreach (var grp in SynonymGroups)
        {
            foreach (var t in grp)
            {
                if (!d.TryGetValue(t, out var set))
                {
                    set = new HashSet<string>();
                    d[t] = set;
                }
                foreach (var s in grp)
                    if (s != t) set.Add(s);
            }
        }
        return d;
    }

    private static Dictionary<FieldInfo, HashSet<string>> BuildKeywordSets(
        FieldInfo[] fields,
        Dictionary<string, HashSet<string>> expansion)
    {
        var result = new Dictionary<FieldInfo, HashSet<string>>(fields.Length);
        var tokens = new List<string>();
        foreach (var f in fields)
        {
            tokens.Clear();
            // Tokenizzo il nome del field: "enemyHittedTank" -> [enemy, hitted, tank].
            Tokenize(f.Name, tokens);
            var set = new HashSet<string>();
            foreach (var t in tokens)
            {
                if (t.Length < 2) continue;
                if (Stopwords.Contains(t)) continue;
                set.Add(t);
                // Espando con sinonimi: il keyword set di "enemyHittedTank" includerà
                // anche "hit", "hurt", "wounded", "vehicle".
                if (expansion.TryGetValue(t, out var syns))
                    foreach (var s in syns) set.Add(s);
            }
            result[f] = set;
        }
        return result;
    }

    private static Dictionary<string, float> BuildIdf(
        Dictionary<FieldInfo, HashSet<string>> keywordSets)
    {
        var df = new Dictionary<string, int>();
        foreach (var set in keywordSets.Values)
            foreach (var t in set)
                df[t] = df.TryGetValue(t, out var c) ? c + 1 : 1;

        int N = keywordSets.Count;
        var idf = new Dictionary<string, float>(df.Count);
        foreach (var kv in df)
        {
            float v = Mathf.Log((float)N / kv.Value, 2f);
            // Floor minimo: anche un token presente in tutti i field
            // mantiene un piccolo segnale (log2(1)=0 -> 0.05).
            if (v < 0.05f) v = 0.05f;
            idf[kv.Key] = v;
        }
        return idf;
    }

    /// <summary>
    /// Calcola il miglior fuzzy match per un filename.
    /// Restituisce (field, bestScore, secondScore). Score in [0, 1].
    /// Le soglie di accettazione sono applicate dal chiamante.
    /// </summary>
    private static (FieldInfo bestField, float bestScore, float secondScore) FuzzyMatchField(
        List<string> fnTokens,
        Dictionary<FieldInfo, HashSet<string>> keywordSets,
        Dictionary<string, float> idf,
        Dictionary<string, HashSet<string>> expansion)
    {
        // Filtro i token significativi del filename e calcolo il loro peso totale.
        // Un token è "significativo" se: lunghezza >= 2, non stopword, non cifre pure,
        // e presente in IDF (cioè usato da almeno un field). I token assenti dall'IDF
        // sono tipicamente prefissi voice pack (cad, ind, ita, jp, ...) che vanno
        // ignorati per non sporcare il denominatore.
        float totalWeight = 0f;
        var weighted = new List<(string tok, float w)>(fnTokens.Count);
        foreach (var t in fnTokens)
        {
            if (t.Length < 2) continue;
            if (Stopwords.Contains(t)) continue;
            if (IsPureDigits(t)) continue;
            if (!idf.TryGetValue(t, out float w)) continue;
            weighted.Add((t, w));
            totalWeight += w;
        }
        if (totalWeight < 0.5f) return (null, 0f, 0f);

        FieldInfo best = null;
        float bestScore = 0f;
        float secondScore = 0f;

        foreach (var kv in keywordSets)
        {
            var fieldSet = kv.Value;
            float matchWeight = 0f;
            for (int i = 0; i < weighted.Count; i++)
            {
                var (t, w) = weighted[i];
                if (fieldSet.Contains(t))
                {
                    matchWeight += w;
                    continue;
                }
                // Sinonimo: peso ridotto a 0.7x per dare priorità ai match diretti.
                if (expansion.TryGetValue(t, out var syns))
                {
                    foreach (var s in syns)
                    {
                        if (fieldSet.Contains(s))
                        {
                            matchWeight += w * 0.7f;
                            break;
                        }
                    }
                }
            }
            float score = matchWeight / totalWeight;
            if (score > bestScore)
            {
                secondScore = bestScore;
                bestScore = score;
                best = kv.Key;
            }
            else if (score > secondScore)
            {
                secondScore = score;
            }
        }

        return (best, bestScore, secondScore);
    }

    private static bool IsPureDigits(string t)
    {
        if (string.IsNullOrEmpty(t)) return false;
        for (int i = 0; i < t.Length; i++)
            if (!char.IsDigit(t[i])) return false;
        return true;
    }

    /// <summary>
    /// Tokenizza in token lowercased separati da:
    ///  - non-alfanumerico (_, -, spazio, .)
    ///  - lower -> upper (camelCase: "imReloading" -> "im","reloading")
    ///  - lettera <-> cifra ("Move1A" -> "move","1","a")
    /// I run di maiuscole consecutive restano un singolo token ("AAAAAH" -> "aaaaah").
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
                    (char.IsLower(prev) && char.IsUpper(c)) ||
                    (char.IsLetter(prev) && char.IsDigit(c)) ||
                    (char.IsDigit(prev) && char.IsLetter(c));

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