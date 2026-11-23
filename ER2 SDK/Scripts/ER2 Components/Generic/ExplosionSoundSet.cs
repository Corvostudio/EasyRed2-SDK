using UnityEngine;
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
#endif

/// <summary>
/// Sounds of one kind of explosion, to assign to ExplosionManager.soundSet: one set serves every explosion that sounds alike.
/// The sounds are grouped by where the listener is, and the game plays the group that fits:
///  - in the open air: near or distant by the distance, like the two clips of ExplosionManager;
///  - indoors (same state as the reverb of footsteps, voices and guns): indoorNear within indoorNearDistance, indoorDistant beyond.
/// An empty indoor group leaves its place to near or distant. Those two must always be filled: with a set ExplosionManager uses nothing else.
/// The sounds are assigned as AudioClip, but only in the editor: the game loads each one by name from a bundle, the first time it is
/// needed. Sync writes the names and puts the clips in that bundle: for the sets of a mod the Mod Compiler does it, with the bundle of the mod
/// </summary>
[CreateAssetMenu(fileName = "ExplosionSounds", menuName = "ScriptableObjects/Explosion Sound Set")]
public partial class ExplosionSoundSet : ScriptableObject
{
    [System.Serializable]
    public partial class Sounds
    {
#if UNITY_EDITOR
        public AudioClip[] clips = new AudioClip[0];//editor only: the build keeps the names, so the clips stay out of the bundle of whoever uses the set
#endif
        [HideInInspector] public string[] names = new string[0];//names of the clips inside the bundle (Sync)
    }

    [Tooltip("Listener in the open air, explosion closer than the near sound distance of the explosion")]
    public Sounds near = new Sounds();
    [Tooltip("Listener in the open air, explosion beyond the near sound distance of the explosion")]
    public Sounds distant = new Sounds();
    [Tooltip("Listener indoors: an explosion closer than this plays Indoor Near, a farther one Indoor Distant")]
    public float indoorNearDistance = 40;
    [Tooltip("Listener indoors (same state as the reverb of footsteps, voices and guns), explosion closer than Indoor Near Distance. Empty: Near or Distant is used")]
    public Sounds indoorNear = new Sounds();
    [Tooltip("Listener indoors, explosion beyond Indoor Near Distance. Empty: Near or Distant is used")]
    public Sounds indoorDistant = new Sounds();
    [HideInInspector] public string modBundleName = "";//bundle of the mod the sounds are in, written by the Mod Compiler. Empty: the sounds bundle of the game

#if UNITY_EDITOR
    /// <summary>
    /// Sync for the set of a mod: the sounds go in the bundle of the mod, and the set remembers which one it is
    /// </summary>
    public void SyncForMod(string bundle)
    {
        if (modBundleName != bundle)
        {
            modBundleName = bundle;
            EditorUtility.SetDirty(this);
        }
        Sync(new[] { this }, bundle);
    }

    /// <summary>
    /// For every clip of the sets: puts it in the bundle, gives it the optimized import settings of the voices and writes its name in
    /// the set. Then reports the names that point to more than one asset of the bundle
    /// </summary>
    public static void Sync(IEnumerable<ExplosionSoundSet> sets, string bundle)
    {
        HashSet<string> used = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (ExplosionSoundSet set in sets)
        {
            if (!set)
                continue;
            foreach (Sounds sounds in new[] { set.near, set.distant, set.indoorNear, set.indoorDistant })
            {
                AudioClip[] clips = sounds.clips.Where(clip => clip != null).ToArray();
                foreach (AudioClip clip in clips)
                {
                    string path = AssetDatabase.GetAssetPath(clip);
                    AssetImporter importer = AssetImporter.GetAtPath(path);
                    if (importer.assetBundleName != bundle)
                    {
                        if (importer.assetBundleName != "")
                            Debug.LogWarning("Explosion sound set '" + set.name + "': '" + path + "' moves from the bundle '" + importer.assetBundleName + "' to '" + bundle + "'. Whatever loaded it by name from the first one will not find it any more.", set);
                        importer.assetBundleName = bundle;
                    }
                    VoiceManager.OptimizeImportSettings(clip);//reimports only if the settings are not those already
                }
                string[] names = System.Array.ConvertAll(clips, clip => clip.name);
                used.UnionWith(names);
                if (!names.SequenceEqual(sounds.names))
                {
                    sounds.names = names;
                    EditorUtility.SetDirty(set);
                }
            }
        }

        //in a bundle a name must point to one asset only, or the game can load the wrong one
        foreach (IGrouping<string, string> sameName in AssetDatabase.GetAssetPathsFromAssetBundle(bundle).GroupBy(assetPath => Path.GetFileNameWithoutExtension(assetPath), System.StringComparer.OrdinalIgnoreCase))
            if (sameName.Count() > 1 && used.Contains(sameName.Key))
                Debug.LogError("Bundle '" + bundle + "': more than one asset is named '" + sameName.Key + "', and the explosion sound sets load it by name: " + string.Join(", ", sameName));

        AssetDatabase.SaveAssets();
    }
#endif
}

#if UNITY_EDITOR
/// <summary>
/// A group of sounds is shown as the list of its clips: the names are written by ExplosionSoundSet.Sync
/// </summary>
[CustomPropertyDrawer(typeof(ExplosionSoundSet.Sounds))]
public class ExplosionSoundsDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("clips"), label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.PropertyField(position, property.FindPropertyRelative("clips"), label, true);
    }
}
#endif
