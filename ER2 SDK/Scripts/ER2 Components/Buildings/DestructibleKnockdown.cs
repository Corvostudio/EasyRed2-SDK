using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A prop that goes down when a vehicle drives into it, such as a tree: the prop itself lies on the ground, so it needs no destroyed
/// version and no effect of its own. Goes on the root of the prop with the Destructible Manager, in place of a phased part.
/// The prop tilts around its origin, which must be at its foot.
/// </summary>
public partial class DestructibleKnockdown : DestructableBuilding
{
    [Header("Knockdown")]
    [Tooltip("Lightest vehicle that knocks it down just by touching it, by rigidbody mass: cars and trucks are about 1200, armoured cars and half-tracks 2000 to 3000, tanks 5000.")]
    public float minVehicleMass = 2500;
    [Tooltip("Sound played when it goes down, from the er2bundle bundle. Empty = none.")]
    public string fallSound = "tree_fall";

    //The fall, in three parts: the prop gathers speed from standing still (it has inertia, whatever the speed of the vehicle: a fast
    //one is through it before it is down), slows down as it reaches the ground, then bounces once. The time it takes grows with the
    //size of the prop and shrinks, little by little, with the speed of the vehicle that pushes it.
    private const float SELF_TIME = 2;//seconds to reach the ground by itself (gunfire, explosions, a vehicle that hardly moves), at REFERENCE_SIZE
    private const float PUSHED_TIME = .85f;//seconds to reach the ground under a vehicle at FULL_PUSH or faster, at REFERENCE_SIZE: a vehicle never makes it shorter
    private const float FULL_PUSH = 8;//m/s, about 30 km/h
    private const float REFERENCE_SIZE = 15;//metres, a tall tree: the times follow the square root of the size, as the fall of a longer trunk does
    private const float LANDING = .75f;//speed left when it touches the ground, against its average speed: 0 = it lands softly, 2 = it never slows down
    private const float BOUNCE = .035f;//height of the bounce, as a share of the fall
    private const float BOUNCE_TIME = .85f;//length of the bounce, as a share of the time of the fall: a third to rise, the rest to settle

    public const float FALL_END = 1 + BOUNCE_TIME;//when the prop is at rest, counted in times to reach the ground
    private const int GROUND_SAMPLES = 4;//points along the prop where it can come to rest on the ground

    /// <summary>How long the prop is, in metres: the size of its LOD Group, as scaled. The reference size where it has none.</summary>
    private float Size { get { return TryGetComponent(out LODGroup lod) ? lod.size * transform.lossyScale.y : REFERENCE_SIZE; } }

    /// <summary>Seconds this prop takes to reach the ground under a vehicle at that speed (0 = it falls by itself).</summary>
    public float GroundTime(float speed)
    {
        return Mathf.Lerp(SELF_TIME, PUSHED_TIME, speed / FULL_PUSH) * Mathf.Sqrt(Size / REFERENCE_SIZE);
    }

    /// <summary>How far down the prop is (0 upright, 1 lying) along the fall: t counts the times to reach the ground, so 1 = it touches it and FALL_END = it is at rest.</summary>
    public static float FallAmount(float t)
    {
        if (t < 1)
            return t * t * (3 - LANDING + (LANDING - 2) * t);//from rest, fastest a little past half way, slower at the ground
        t = Mathf.Min((t - 1) / BOUNCE_TIME, 1);
        return 1 - 6.75f * BOUNCE * t * (1 - t) * (1 - t);//up, then back down slower and slower: the crown cushions the landing
    }

    /// <summary>Tilts the prop around its origin toward a compass direction (0 = +Z, 90 = +X), by an angle from its upright rotation.</summary>
    public void Tilt(Quaternion upright, float yaw, float angle)
    {
        Vector3 direction = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.Cross(Vector3.up, direction)) * upright;
    }

    /// <summary>
    /// The angle that lays the prop on the ground toward a compass direction, however it leans and whatever the ground is like: it
    /// goes down until a point of its length touches the terrain, taken as level where the scene has none. The foot counts as
    /// standing on the ground. Found by halving: ten times get within a fifth of a degree. To be asked while the prop still stands.
    /// </summary>
    public float LyingAngle(float yaw)
    {
        Vector3 axis = Quaternion.Euler(0, yaw, 0) * Vector3.right, standing = transform.up * Size;
        Terrain terrain = Terrain.activeTerrain;
#if UNITY_EDITOR
        if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null)
            terrain = null;//a prefab being edited sits in a scene of its own: the terrain of the scene left open behind is not under it
#endif
        float foot = terrain ? terrain.SampleHeight(transform.position) : 0, clear = 0, touching = 180;
        for (int step = 0; step < 10; step++)
        {
            float angle = (clear + touching) / 2;
            Vector3 turned = Quaternion.AngleAxis(angle, axis) * standing;
            bool touches = false;
            for (int i = 1; i <= GROUND_SAMPLES && !touches; i++)
            {
                Vector3 point = turned * i / GROUND_SAMPLES;
                touches = point.y <= (terrain ? terrain.SampleHeight(transform.position + point) - foot : 0);
            }
            if (touches) touching = angle;
            else clear = angle;
        }
        return touching;
    }

    /// <summary>Knocks the prop down for real, where the game code is there to do it (the inspector test while playing).</summary>
    public bool KnockInGame(float yaw, float speed) { bool done = false; KnockInGame(yaw, speed, ref done); return done; }
    partial void KnockInGame(float yaw, float speed, ref bool done);
}

#if UNITY_EDITOR
[CustomEditor(typeof(DestructibleKnockdown)), CanEditMultipleObjects]
public class DestructibleKnockdownEditor : Editor
{
    //Test: the prop goes down in the scene as it does in game, stays there a moment and stands back up. Nothing is saved.
    private const float holdTime = 2f;
    private static float testYaw, testSpeedKmh = 20, testAngle;
    private static DestructibleKnockdown tested;
    private static Quaternion testUpright;
    private static double testStart;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (targets.Length != 1)
            return;

        var prop = (DestructibleKnockdown)target;
        if (!prop.GetComponentInParent<DestructibleManager>(true))
            EditorGUILayout.HelpBox("Missing DestructibleManager on the root of the prop!", MessageType.Error);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Knockdown test", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        testYaw = EditorGUILayout.Slider(new GUIContent("Direction", "Compass direction the prop falls toward: 0 = +Z, 90 = +X. The arrow in the scene shows it."), testYaw, 0, 359);
        if (EditorGUI.EndChangeCheck())
            SceneView.RepaintAll();
        testSpeedKmh = EditorGUILayout.Slider(new GUIContent("Vehicle speed (km/h)", "Speed of the vehicle that pushes it down: the faster it is, the sooner the prop is down, up to about 30 km/h. 0 = it falls by itself, as under gunfire or an explosion."), testSpeedKmh, 0, 60);
        EditorGUILayout.LabelField(new GUIContent("Reaches the ground in", "At this speed. Bigger props take longer: the size is the one of the LOD Group of the prop."), new GUIContent(prop.GroundTime(testSpeedKmh / 3.6f).ToString("0.00") + " s"));
        using (new EditorGUI.DisabledScope(tested))
            if (GUILayout.Button(Application.isPlaying ? "Knock down (for real, synced)" : "Knock down") && !(Application.isPlaying && prop.KnockInGame(testYaw, testSpeedKmh / 3.6f)))
                StartTest(prop);
        EditorGUILayout.HelpBox(Application.isPlaying
            ? "While playing the prop goes down for real, as if a vehicle had hit it at that speed."
            : "Preview only: the prop stands back up after two seconds and nothing is saved. Drive into it in game to check the vehicle side.", MessageType.None);
    }

    private static double refusedAt;//when a conversion was last refused: one notice for a whole selection

    //from a phased part (the trees so far) in one click: same life and damage settings, no phases and no destroyed version
    [MenuItem("CONTEXT/DestructibleBuildingPhased/Convert to Destructible Knockdown")]
    private static void ConvertPhased(MenuCommand command)
    {
        var phased = (DestructibleBuildingPhased)command.context;
        if (!phased)
            return;//gone with the prefab it came from, converted a moment ago in the same selection

        //A part that comes from another prefab (this one is a variant of it, or an object made from it) is converted there, and this
        //one follows. Converted here it would get a knockdown part of its own, and a second one when that prefab is converted too.
        Object origin = PrefabUtility.GetCorrespondingObjectFromSource(phased);
        if (origin)
        {
            while (PrefabUtility.GetCorrespondingObjectFromSource(origin))
                origin = PrefabUtility.GetCorrespondingObjectFromSource(origin);
            if (EditorApplication.timeSinceStartup - refusedAt > 1)
                EditorUtility.DisplayDialog("Convert to Destructible Knockdown", "This part comes from the prefab '" + System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(origin)) + "'.\n\nConvert that prefab: every prefab and object made from it follows. Nothing was changed here.", "OK");
            refusedAt = EditorApplication.timeSinceStartup;
            return;
        }

        var knockdown = Undo.AddComponent<DestructibleKnockdown>(phased.gameObject);
        knockdown.life = phased.life;
        knockdown.minBulletPenetration = phased.minBulletPenetration;
        knockdown.bulletDamageMultiplier = phased.bulletDamageMultiplier;
        knockdown.fireDamageMultiplier = phased.fireDamageMultiplier;
        knockdown.explosionDamageMultiplier = phased.explosionDamageMultiplier;
        knockdown.vehicleCollisionMultiplier = phased.vehicleCollisionMultiplier;
        var manager = phased.GetComponentInParent<DestructibleManager>(true);
        string destroyedVersion = phased.onPhaseChangeEffect;
        bool prefabConverted = EditorUtility.IsPersistent(phased) || UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(phased.gameObject) != null;
        if (manager)
        {
            //on the list of the manager the knockdown part takes the place of the phased one, so the other parts keep theirs and the
            //states saved for each still match
            Undo.RecordObject(manager, "Convert to Destructible Knockdown");
            var parts = new List<DestructableBuilding>(manager.destructibleParts);
            parts.Remove(knockdown);
            int slot = parts.IndexOf(phased);
            if (slot >= 0) parts[slot] = knockdown;
            else parts.Add(knockdown);
            manager.destructibleParts = parts.ToArray();
            EditorUtility.SetDirty(manager);
        }
        Undo.DestroyObjectImmediate(phased);
        if (destroyedVersion.Length > 0)
            OfferToTrash(destroyedVersion, prefabConverted);
    }

    //the destroyed version the phased part spawned (its _DST prefab) is out of use: to the trash, if wanted, with the prefabs made
    //from it (other destroyed versions that are its variants), which would be left broken
    private static void OfferToTrash(string destroyedVersion, bool prefabConverted)
    {
        foreach (string guid in AssetDatabase.FindAssets(destroyedVersion + " t:prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != destroyedVersion)
                continue;

            var paths = new List<string> { path };
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (string otherGuid in AssetDatabase.FindAssets("t:prefab", new[] { path.Substring(0, path.LastIndexOf('/')) }))
            {
                string otherPath = AssetDatabase.GUIDToAssetPath(otherGuid);
                for (GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(otherPath); source;)
                    if ((source = PrefabUtility.GetCorrespondingObjectFromSource(source)) == prefab)
                    {
                        paths.Add(otherPath);
                        break;
                    }
            }

            string message = "'" + destroyedVersion + "' was the destroyed version of this prop: the knockdown does not use it. Move it to the trash?\n\n" + string.Join("\n", paths);
            if (paths.Count > 1)
                message += "\n\nThe prefabs after the first are made from it and would be left broken: they go with it.";
            if (!prefabConverted)
                message += "\n\nCareful: this is an object of a scene. Its prefab is not converted and still spawns it.";
            message += "\n\nKeep it if another prop still names it as its On Phase Change Effect.";
            if (EditorUtility.DisplayDialog("Convert to Destructible Knockdown", message, "Move to trash", "Keep"))
                foreach (string trashed in paths)
                    AssetDatabase.MoveAssetToTrash(trashed);
        }
    }

    private void OnSceneGUI()
    {
        Vector3 foot = ((DestructibleKnockdown)target).transform.position;
        Handles.color = new Color(1f, .45f, 0f);
        Handles.ArrowHandleCap(0, foot + Vector3.up * .3f, Quaternion.Euler(0, testYaw, 0), HandleUtility.GetHandleSize(foot) * 1.5f, EventType.Repaint);
        Handles.color = Color.white;
    }

    private static void StartTest(DestructibleKnockdown prop)
    {
        StopTest();
        tested = prop;
        testUpright = prop.transform.rotation;
        testAngle = prop.LyingAngle(testYaw);
        testStart = EditorApplication.timeSinceStartup;
        EditorApplication.update += UpdateTest;
        EditorApplication.playModeStateChanged += StopTest;
        AssemblyReloadEvents.beforeAssemblyReload += StopTest;
        Selection.selectionChanged += StopTest;
    }
    private static void UpdateTest()
    {
        float time = (float)(EditorApplication.timeSinceStartup - testStart), groundTime = tested ? tested.GroundTime(testSpeedKmh / 3.6f) : 0;
        if (!tested || time > groundTime * DestructibleKnockdown.FALL_END + holdTime)
        {
            StopTest();
            return;
        }
        tested.Tilt(testUpright, testYaw, testAngle * DestructibleKnockdown.FallAmount(time / groundTime));
        SceneView.RepaintAll();
    }
    private static void StopTest(PlayModeStateChange change) { StopTest(); }
    private static void StopTest()
    {
        EditorApplication.update -= UpdateTest;
        EditorApplication.playModeStateChanged -= StopTest;
        AssemblyReloadEvents.beforeAssemblyReload -= StopTest;
        Selection.selectionChanged -= StopTest;
        if (tested)
            tested.transform.rotation = testUpright;
        tested = null;
        SceneView.RepaintAll();
    }
}
#endif
