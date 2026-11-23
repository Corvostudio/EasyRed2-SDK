using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sound of an explosion effect: goes on the prefab of the effect and plays when it is spawned, the near or the distant sound by
/// how far the camera is. The sounds come from the sound set or, without one, from the two clips
/// </summary>
public partial class ExplosionManager : AutoSoundPlayer
{
    [Tooltip("Sounds picked by where the listener is (open air or indoors) and by distance. With a set assigned, the two clips below are unlinked and hidden.")]
    public ExplosionSoundSet soundSet;
    [Tooltip("Sound of an explosion closer than Near Sound Distance. Used only without a Sound Set.")]
    public AudioClip ExplodeAudio_near;
    [Tooltip("Sound of an explosion beyond Near Sound Distance. Used only without a Sound Set.")]
    public AudioClip ExplodeAudio_distant;
    [Tooltip("Distance from the camera, in metres, within which the near sound plays. Beyond it the distant sound plays, delayed by the speed of sound.")]
    [Range(1,1000)]
    public int near_sound_distance = 80;
    [Tooltip("Volume of the sound, multiplied by the effects volume of the player.")]
    [Range(0, 2)]
    public float volume = .85f;

    [Tooltip("Maximum distance, in metres, at which the sound is heard.")]
    [Range(0, 2000)]
    public float max_distance = 500f;


    [HideInInspector]//deprecated: not read by the game, far explosions are always delayed by the distance
    public bool delayWithDistance = true;
}

#if UNITY_EDITOR
/// <summary>
/// With a sound set assigned the two clips are not used: they are hidden and unlinked, so they do not end up in the bundle of the prefab
/// </summary>
[CustomEditor(typeof(ExplosionManager), true)]
[CanEditMultipleObjects]
public class ExplosionManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty set = serializedObject.FindProperty("soundSet");
        bool withSet = set.objectReferenceValue != null || set.hasMultipleDifferentValues;//at least one of the selected objects has a set
        SerializedProperty property = serializedObject.GetIterator();
        for (bool enterChildren = true; property.NextVisible(enterChildren); enterChildren = false)
        {
            if (withSet && (property.name == "ExplodeAudio_near" || property.name == "ExplodeAudio_distant"))
                continue;
            using (new EditorGUI.DisabledScope(property.name == "m_Script"))
                EditorGUILayout.PropertyField(property, true);
        }
        if (set.objectReferenceValue != null && !set.hasMultipleDifferentValues)//all the selected objects have the same set: the clips are unlinked
        {
            serializedObject.FindProperty("ExplodeAudio_near").objectReferenceValue = null;
            serializedObject.FindProperty("ExplodeAudio_distant").objectReferenceValue = null;
        }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
