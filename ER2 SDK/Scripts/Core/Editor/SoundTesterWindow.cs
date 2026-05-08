#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor window for testing sounds the way GenericGun plays them at runtime.
/// 
/// Modes:
///   - Simple     : one-shot clip, optional distant variant
///   - GunSingle  : single shots, optional hold-to-fire at RPM
///   - Looped     : start / loop / tail (close), optional distant loop+tail (no start)
/// 
/// Distance rules mirror GenericGun:
///   d <= minDist                                    -> close clip
///   minDist < d <= maxDist  AND distant clip exists -> distant clip
///   minDist < d <= maxDist  AND no distant clip     -> close clip (gun fallback)
///   d > maxDist                                     -> silent
/// 
/// Pitch rules mirror GenericGun:
///   single shots: pitch = pitchStatic * Random(randMin, randMax)
///   loops:        pitch = pitchStatic
/// 
/// HOW TO USE
///   1. Place this file in any "Editor" folder.
///   2. Open Window > Audio > Sound Tester.
///   3. Enable Scene View audio (speaker icon in Scene toolbar) so the Scene
///      camera acts as the listener. Keep the camera near world origin so the
///      graph (centered on the listener) matches what you actually hear.
///   4. Assign clips, drag the dot in the 2D graph to move the emitter around
///      in world space (graph X = world X, graph Y = world Z).
/// </summary>
public class SoundTesterWindow : EditorWindow
{
    enum SoundMode { Simple, GunSingle, Looped }
    enum LoopState { Idle, Intro, Body, Tail }

    // ── Mode ──
    [SerializeField] SoundMode mode = SoundMode.Simple;

    // ── Clips: simple / gun-single ──
    [SerializeField] AudioClip clipClose;
    [SerializeField] AudioClip clipDistant;

    // ── Clips: looped close ──
    [SerializeField] AudioClip loopStart;
    [SerializeField] AudioClip loopBody;
    [SerializeField] AudioClip loopTail;

    // ── Clips: looped distant (no start) ──
    [SerializeField] AudioClip loopDistantBody;
    [SerializeField] AudioClip loopDistantTail;

    // ── Distance switch ──
    [SerializeField] bool useDistantVariant = false;
    [SerializeField] float distantMinDist = 50f;
    [SerializeField] float distantMaxDist = 250f;

    // ── 3D rolloff ──
    [SerializeField] float audioMaxDistance = 250f;
    [SerializeField] AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;
    [SerializeField] AnimationCurve customRolloff = AnimationCurve.Linear(0, 1, 1, 0);
    [SerializeField] float volumeClose = 0.5f;
    [SerializeField] float volumeDistant = 0.5f;
    [SerializeField] float spatialBlend = 1f;
    [SerializeField] bool autoSpatialBlend = false;
    [SerializeField, Range(0f, 1f)] float farDistance2dValue = 0.4f;

    // ── Pitch ──
    [SerializeField] float pitchStatic = 1f;
    [SerializeField] float pitchRandomMin = 0.984f;
    [SerializeField] float pitchRandomMax = 1.016f;

    // ── Gun single ──
    [SerializeField] int rpm = 600;

    // ── 2D graph ──
    [SerializeField] Vector2 emitterPos = new Vector2(0, 30);
    [SerializeField] float graphMaxRange = 500f;

    // ── Runtime ──
    GameObject emitterGO;
    AudioSource source;
    LoopState loopState = LoopState.Idle;
    double introEndTime;
    bool stateIsClose = true;
    bool firingHeld = false;
    double lastShotTime = -999;
    float lastRandomRoll = 1f;
    Vector2 scroll;

    [MenuItem("ER2 TOOLS/Tools/Sound Tester Window")]
    public static void Open()
    {
        var w = GetWindow<SoundTesterWindow>("Sound Tester");
        w.minSize = new Vector2(380, 640);
        w.Show();
    }

    void OnEnable()
    {
        EnsureEmitter();
        ApplySourceSettings();
        EditorApplication.update += EditorTick;
    }

    void OnDisable()
    {
        EditorApplication.update -= EditorTick;
        StopAll();
        if (emitterGO != null)
        {
            DestroyImmediate(emitterGO);
            emitterGO = null;
            source = null;
        }
    }

    void EnsureEmitter()
    {
        if (emitterGO == null)
        {
            emitterGO = new GameObject("__SoundTester_Emitter");
            emitterGO.hideFlags = HideFlags.HideAndDontSave;
            source = emitterGO.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
        }
    }

    void ApplySourceSettings()
    {
        if (source == null) return;
        if (!autoSpatialBlend) source.spatialBlend = spatialBlend;
        source.maxDistance = audioMaxDistance;
        source.rolloffMode = rolloff;
        if (rolloff == AudioRolloffMode.Custom)
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customRolloff);
    }

    // ──────────── GUI ────────────
    void OnGUI()
    {
        EnsureEmitter();
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUI.BeginChangeCheck();
        var newMode = (SoundMode)EditorGUILayout.EnumPopup("Mode", mode);
        if (newMode != mode) { StopAll(); mode = newMode; }
        EditorGUILayout.Space();

        DrawClipsSection();
        EditorGUILayout.Space();
        DrawDistanceSection();
        EditorGUILayout.Space();
        DrawAudioSection();
        EditorGUILayout.Space();
        DrawPitchSection();
        EditorGUILayout.Space();

        if (mode == SoundMode.GunSingle)
        {
            EditorGUILayout.LabelField("Gun-single", EditorStyles.boldLabel);
            rpm = Mathf.Max(10, EditorGUILayout.IntField("RPM (hold-to-fire)", rpm));
            EditorGUILayout.Space();
        }

        DrawGraphSection();
        EditorGUILayout.Space();
        DrawPlaybackSection();

        if (EditorGUI.EndChangeCheck())
            ApplySourceSettings();

        EditorGUILayout.EndScrollView();
    }

    void DrawClipsSection()
    {
        EditorGUILayout.LabelField("Clips", EditorStyles.boldLabel);
        if (mode == SoundMode.Looped)
        {
            loopStart = (AudioClip)EditorGUILayout.ObjectField("Loop start (close)", loopStart, typeof(AudioClip), false);
            loopBody  = (AudioClip)EditorGUILayout.ObjectField("Loop body  (close)", loopBody,  typeof(AudioClip), false);
            loopTail  = (AudioClip)EditorGUILayout.ObjectField("Loop tail  (close)", loopTail,  typeof(AudioClip), false);
        }
        else
        {
            clipClose = (AudioClip)EditorGUILayout.ObjectField("Close clip", clipClose, typeof(AudioClip), false);
        }

        useDistantVariant = EditorGUILayout.Toggle("Use distant variant", useDistantVariant);
        if (useDistantVariant)
        {
            EditorGUI.indentLevel++;
            if (mode == SoundMode.Looped)
            {
                loopDistantBody = (AudioClip)EditorGUILayout.ObjectField("Distant loop body", loopDistantBody, typeof(AudioClip), false);
                loopDistantTail = (AudioClip)EditorGUILayout.ObjectField("Distant loop tail", loopDistantTail, typeof(AudioClip), false);
            }
            else
            {
                clipDistant = (AudioClip)EditorGUILayout.ObjectField("Distant clip", clipDistant, typeof(AudioClip), false);
            }
            EditorGUI.indentLevel--;
        }
    }

    void DrawDistanceSection()
    {
        EditorGUILayout.LabelField("Distance switch", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(!useDistantVariant))
        {
            distantMinDist = Mathf.Max(0, EditorGUILayout.FloatField("Min dist (close → distant)", distantMinDist));
            distantMaxDist = Mathf.Max(distantMinDist + 0.01f, EditorGUILayout.FloatField("Max dist (cutoff)", distantMaxDist));
        }
    }

    void DrawAudioSection()
    {
        EditorGUILayout.LabelField("AudioSource", EditorStyles.boldLabel);
        autoSpatialBlend = EditorGUILayout.Toggle(new GUIContent("Auto spatial blend",
            "Mimics GenericGun.FixAudioSourceVolumes: 1 when close, lerps toward 'farDistance2dValue' when distant."),
            autoSpatialBlend);
        using (new EditorGUI.DisabledScope(autoSpatialBlend))
            spatialBlend = EditorGUILayout.Slider("Spatial Blend", spatialBlend, 0f, 1f);
        if (autoSpatialBlend)
            farDistance2dValue = EditorGUILayout.Slider("Far 2D value", farDistance2dValue, 0f, 1f);

        audioMaxDistance = Mathf.Max(1, EditorGUILayout.FloatField("Max Distance (rolloff)", audioMaxDistance));
        rolloff          = (AudioRolloffMode)EditorGUILayout.EnumPopup("Rolloff Mode", rolloff);
        if (rolloff == AudioRolloffMode.Custom)
            customRolloff = EditorGUILayout.CurveField("Custom Rolloff", customRolloff);
        volumeClose   = EditorGUILayout.Slider("Volume (close)",   volumeClose,   0f, 1f);
        volumeDistant = EditorGUILayout.Slider("Volume (distant)", volumeDistant, 0f, 1f);
    }

    void DrawPitchSection()
    {
        EditorGUILayout.LabelField("Pitch", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        pitchStatic = EditorGUILayout.Slider("Static pitch", pitchStatic, 0.1f, 3f);
        EditorGUILayout.LabelField("Per-shot random multiplier (single shots only)", EditorStyles.miniLabel);
        EditorGUI.indentLevel++;
        pitchRandomMin = EditorGUILayout.FloatField("Min", pitchRandomMin);
        pitchRandomMax = EditorGUILayout.FloatField("Max", pitchRandomMax);
        if (pitchRandomMax < pitchRandomMin) pitchRandomMax = pitchRandomMin;
        EditorGUI.indentLevel--;
        if (EditorGUI.EndChangeCheck())
            ApplyLivePitch();
    }

    /// <summary>Live-update pitch on any currently playing source (loop body, tail, or sustaining single shot).</summary>
    void ApplyLivePitch()
    {
        if (source == null || !source.isPlaying) return;
        source.pitch = pitchStatic * lastRandomRoll;
    }

    void DrawGraphSection()
    {
        EditorGUILayout.LabelField("3D position (top-down)", EditorStyles.boldLabel);
        graphMaxRange = Mathf.Max(10, EditorGUILayout.FloatField("Graph half-extent (m)", graphMaxRange));

        Rect r = GUILayoutUtility.GetRect(320, 320, GUILayout.ExpandWidth(false));
        r = new Rect(r.x + 8, r.y, 320, 320);

        EditorGUI.DrawRect(r, new Color(0.13f, 0.13f, 0.14f));
        Event e = Event.current;

        if (e.type == EventType.Repaint)
        {
            GUI.BeginClip(r);
            Vector2 c = new Vector2(r.width * 0.5f, r.height * 0.5f);

            // Cross axes
            Handles.color = new Color(0.32f, 0.32f, 0.32f);
            Handles.DrawAAPolyLine(1f, new Vector3(c.x, 0, 0), new Vector3(c.x, r.height, 0));
            Handles.DrawAAPolyLine(1f, new Vector3(0, c.y, 0), new Vector3(r.width, c.y, 0));

            // Range circles
            if (useDistantVariant)
            {
                DrawCircleAt(c, MetersToPixels(distantMinDist, r), new Color(0.35f, 0.85f, 0.4f, 0.95f));
                DrawCircleAt(c, MetersToPixels(distantMaxDist, r), new Color(0.85f, 0.35f, 0.35f, 0.95f));
            }
            else
            {
                DrawCircleAt(c, MetersToPixels(audioMaxDistance, r), new Color(0.45f, 0.6f, 0.85f, 0.85f));
            }

            // Listener crosshair
            Handles.color = new Color(0.35f, 0.85f, 0.95f);
            Handles.DrawAAPolyLine(2f, new Vector3(c.x - 8, c.y, 0), new Vector3(c.x + 8, c.y, 0));
            Handles.DrawAAPolyLine(2f, new Vector3(c.x, c.y - 8, 0), new Vector3(c.x, c.y + 8, 0));

            // Emitter dot
            Vector2 ep = WorldToGraphLocal(emitterPos, r);
            EditorGUI.DrawRect(new Rect(ep.x - 5, ep.y - 5, 10, 10), GetStateColor());

            GUI.EndClip();
        }

        // Drag input (absolute coords)
        int id = GUIUtility.GetControlID(FocusType.Passive);
        switch (e.GetTypeForControl(id))
        {
            case EventType.MouseDown:
                if (r.Contains(e.mousePosition) && e.button == 0)
                {
                    GUIUtility.hotControl = id;
                    emitterPos = ClampToGraph(GraphToWorld(e.mousePosition, r));
                    e.Use();
                    Repaint();
                }
                break;
            case EventType.MouseDrag:
                if (GUIUtility.hotControl == id)
                {
                    emitterPos = ClampToGraph(GraphToWorld(e.mousePosition, r));
                    e.Use();
                    Repaint();
                }
                break;
            case EventType.MouseUp:
                if (GUIUtility.hotControl == id)
                {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;
        }

        GUILayout.Space(8);
        float dist = emitterPos.magnitude;
        EditorGUILayout.LabelField($"Position: x={emitterPos.x:F1} m, z={emitterPos.y:F1} m   |   Distance: {dist:F1} m");
        EditorGUILayout.LabelField($"State: {GetStateLabel()}");

        if (GUILayout.Button("Reset emitter (0, 0)"))
            emitterPos = Vector2.zero;

        EditorGUILayout.HelpBox(
            "Enable Scene View audio (speaker icon in Scene toolbar). The Scene camera is the listener; " +
            "the emitter sits at world (graphX, 0, graphY). Keep the Scene camera near world origin so the graph matches what you hear.",
            MessageType.Info);
    }

    void DrawPlaybackSection()
    {
        EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
        switch (mode)
        {
            case SoundMode.Simple:
                using (new EditorGUI.DisabledScope(clipClose == null))
                {
                    if (GUILayout.Button("▶ Play once", GUILayout.Height(30)))
                        PlaySingle();
                }
                break;

            case SoundMode.GunSingle:
                using (new EditorGUI.DisabledScope(clipClose == null))
                {
                    if (GUILayout.Button("▶ Single shot", GUILayout.Height(24)))
                        PlaySingle();

                    string holdLabel = firingHeld ? "■ FIRING — click to release" : $"▶ Hold to fire @ {rpm} RPM";
                    GUI.backgroundColor = firingHeld ? new Color(0.95f, 0.5f, 0.5f) : Color.white;
                    if (GUILayout.Button(holdLabel, GUILayout.Height(36)))
                    {
                        firingHeld = !firingHeld;
                        if (firingHeld) lastShotTime = -999;
                    }
                    GUI.backgroundColor = Color.white;
                }
                break;

            case SoundMode.Looped:
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(loopState != LoopState.Idle || (loopBody == null && loopDistantBody == null)))
                {
                    if (GUILayout.Button("▶ Start loop", GUILayout.Height(34)))
                        StartLoop();
                }
                using (new EditorGUI.DisabledScope(loopState == LoopState.Idle))
                {
                    if (GUILayout.Button("⏹ Stop loop", GUILayout.Height(34)))
                        StopLoop();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.LabelField($"Loop state: {loopState}", EditorStyles.miniLabel);
                break;
        }

        if (GUILayout.Button("⏹ Stop all"))
            StopAll();
    }

    // ──────────── helpers ────────────
    Vector2 WorldToGraphLocal(Vector2 wp, Rect r)
    {
        float halfW = r.width * 0.5f;
        float halfH = r.height * 0.5f;
        return new Vector2(halfW + (wp.x / graphMaxRange) * halfW,
                           halfH - (wp.y / graphMaxRange) * halfH);
    }
    Vector2 GraphToWorld(Vector2 sp, Rect r)
    {
        float halfW = r.width * 0.5f;
        float halfH = r.height * 0.5f;
        return new Vector2(((sp.x - r.center.x) / halfW) * graphMaxRange,
                          -((sp.y - r.center.y) / halfH) * graphMaxRange);
    }
    Vector2 ClampToGraph(Vector2 wp)
    {
        return new Vector2(Mathf.Clamp(wp.x, -graphMaxRange, graphMaxRange),
                           Mathf.Clamp(wp.y, -graphMaxRange, graphMaxRange));
    }
    float MetersToPixels(float meters, Rect r) => (meters / graphMaxRange) * (r.width * 0.5f);

    static void DrawCircleAt(Vector2 center, float radius, Color c)
    {
        if (radius < 1f) return;
        Handles.color = c;
        const int seg = 96;
        Vector3[] pts = new Vector3[seg + 1];
        for (int i = 0; i <= seg; i++)
        {
            float a = (i / (float)seg) * Mathf.PI * 2f;
            pts[i] = new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius, 0);
        }
        Handles.DrawAAPolyLine(2f, pts);
    }

    Color GetStateColor()
    {
        float d = emitterPos.magnitude;
        if (useDistantVariant)
        {
            if (d <= distantMinDist) return new Color(0.35f, 0.95f, 0.45f);
            if (d <= distantMaxDist) return new Color(0.95f, 0.75f, 0.25f);
            return new Color(0.6f, 0.6f, 0.6f);
        }
        return d <= audioMaxDistance ? new Color(0.4f, 0.7f, 0.95f) : new Color(0.6f, 0.6f, 0.6f);
    }

    string GetStateLabel()
    {
        float d = emitterPos.magnitude;
        if (useDistantVariant)
        {
            if (d <= distantMinDist) return "CLOSE";
            if (d <= distantMaxDist)
            {
                bool hasDistant = (mode == SoundMode.Looped) ? loopDistantBody != null : clipDistant != null;
                return hasDistant ? "DISTANT" : "CLOSE (no distant clip — fallback)";
            }
            return "OUT OF RANGE — silent";
        }
        return d <= audioMaxDistance ? "PLAYING" : "OUT OF RANGE — silent";
    }

    // ──────────── Distance / clip resolution ────────────
    AudioClip ResolveSingleClip(out bool isClose, out bool inRange)
    {
        float d = emitterPos.magnitude;
        if (!useDistantVariant)
        {
            isClose = true;
            inRange = true;
            return clipClose;
        }
        bool inDistant = d <= distantMaxDist;
        bool inClose   = d <= distantMinDist || (inDistant && clipDistant == null);
        isClose = inClose;
        inRange = inDistant;
        if (inClose)   return clipClose;
        if (inDistant) return clipDistant;
        return null;
    }

    bool ResolveLoopIsClose(out bool inRange)
    {
        float d = emitterPos.magnitude;
        if (!useDistantVariant)
        {
            inRange = true;
            return true;
        }
        bool inDistant = d <= distantMaxDist;
        bool noDistantLoop = loopDistantBody == null;
        bool inClose = d <= distantMinDist || (inDistant && noDistantLoop);
        inRange = inDistant;
        return inClose;
    }

    // ──────────── Pitch / volume / spatial blend ────────────
    void ApplyPerShotState(bool isClose, bool randomizePitch)
    {
        source.volume = isClose ? volumeClose : volumeDistant;
        if (randomizePitch)
            lastRandomRoll = UnityEngine.Random.Range(pitchRandomMin, pitchRandomMax);
        else
            lastRandomRoll = 1f;
        source.pitch = pitchStatic * lastRandomRoll;
        UpdateSpatialBlend(isClose);
    }

    void UpdateSpatialBlend(bool isClose)
    {
        if (!autoSpatialBlend) return;
        float d = emitterPos.magnitude;
        if (isClose || !useDistantVariant)
        {
            source.spatialBlend = 1f;
        }
        else
        {
            // Mirrors GenericGun: spatialBlend = Lerp(farDistance2dValue, 1, (d - minDist) / maxDist)
            float t = Mathf.Clamp01((d - distantMinDist) / Mathf.Max(0.01f, distantMaxDist));
            source.spatialBlend = Mathf.Lerp(farDistance2dValue, 1f, t);
        }
    }

    // ──────────── Single shot ────────────
    void PlaySingle()
    {
        bool isClose, inRange;
        AudioClip c = ResolveSingleClip(out isClose, out inRange);
        if (c == null || !inRange) { source.Stop(); return; }
        ApplyPerShotState(isClose, randomizePitch: true);
        source.loop = false;
        source.clip = c;
        source.Play();
    }

    // ──────────── Loop state machine ────────────
    void StartLoop()
    {
        if (loopState != LoopState.Idle) return;
        bool inRange;
        bool isClose = ResolveLoopIsClose(out inRange);
        if (!inRange) return;

        stateIsClose = isClose;
        if (isClose && loopStart != null)
        {
            ApplyPerShotState(true, randomizePitch: false);
            source.loop = false;
            source.clip = loopStart;
            source.Play();
            introEndTime = EditorApplication.timeSinceStartup + loopStart.length;
            loopState = LoopState.Intro;
        }
        else
        {
            BeginLoopBody(isClose);
        }
    }

    void BeginLoopBody(bool isClose)
    {
        AudioClip body = isClose ? loopBody : loopDistantBody;
        if (body == null) { source.Stop(); loopState = LoopState.Idle; return; }
        ApplyPerShotState(isClose, randomizePitch: false);
        source.loop = true;
        source.clip = body;
        source.Play();
        stateIsClose = isClose;
        loopState = LoopState.Body;
    }

    void StopLoop()
    {
        if (loopState == LoopState.Idle) return;
        bool inRange;
        bool isClose = ResolveLoopIsClose(out inRange);
        AudioClip tail = isClose ? loopTail : loopDistantTail;
        if (inRange && tail != null)
        {
            ApplyPerShotState(isClose, randomizePitch: false);
            source.loop = false;
            source.clip = tail;
            source.Play();
        }
        else
        {
            source.Stop();
        }
        // Return to Idle immediately. The tail (if any) keeps playing out on the
        // AudioSource on its own; pressing Start loop again will simply cut it off.
        loopState = LoopState.Idle;
    }

    void TickLoop()
    {
        if (loopState == LoopState.Intro)
        {
            if (EditorApplication.timeSinceStartup >= introEndTime)
            {
                bool inRange;
                bool isClose = ResolveLoopIsClose(out inRange);
                if (!inRange) { source.Stop(); loopState = LoopState.Idle; }
                else BeginLoopBody(isClose);
            }
        }
        else if (loopState == LoopState.Body)
        {
            bool inRange;
            bool isClose = ResolveLoopIsClose(out inRange);
            if (!inRange)
            {
                if (source.isPlaying) source.Stop();
                // Stay in Body so it can resume if listener moves back into range.
            }
            else
            {
                AudioClip want = isClose ? loopBody : loopDistantBody;
                if (want != null)
                {
                    if (source.clip != want || !source.isPlaying)
                    {
                        ApplyPerShotState(isClose, randomizePitch: false);
                        source.loop = true;
                        source.clip = want;
                        source.Play();
                        stateIsClose = isClose;
                    }
                    else
                    {
                        UpdateSpatialBlend(isClose);
                        source.volume = isClose ? volumeClose : volumeDistant;
                    }
                }
                else if (source.isPlaying) source.Stop();
            }
        }
        else if (loopState == LoopState.Tail)
        {
            if (!source.isPlaying) loopState = LoopState.Idle;
        }
    }

    // ──────────── Tick ────────────
    void EditorTick()
    {
        if (source == null) return;
        emitterGO.transform.position = new Vector3(emitterPos.x, 0, emitterPos.y);

        // While a single shot is playing in Simple/GunSingle, keep volume + spatial blend in sync
        if (mode != SoundMode.Looped && source.isPlaying)
        {
            bool isClose, inRange;
            ResolveSingleClip(out isClose, out inRange);
            source.volume = isClose ? volumeClose : volumeDistant;
            UpdateSpatialBlend(isClose);
        }

        // Hold-to-fire RPM
        if (mode == SoundMode.GunSingle && firingHeld)
        {
            double now = EditorApplication.timeSinceStartup;
            double interval = 60.0 / Mathf.Max(1, rpm);
            if (now - lastShotTime >= interval)
            {
                lastShotTime = now;
                PlaySingle();
            }
        }

        if (mode == SoundMode.Looped) TickLoop();

        Repaint();
    }

    void StopAll()
    {
        if (source != null)
        {
            source.Stop();
            source.clip = null;
            source.loop = false;
        }
        loopState = LoopState.Idle;
        firingHeld = false;
    }
}
#endif
