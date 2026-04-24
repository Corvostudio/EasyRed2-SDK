using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        foreach (AudioClip clip in uniqueClips)
        {
            string path = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrEmpty(path))
                continue;

            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
                return;

            bool changed = false;

            // Check mono
            if (!importer.forceToMono)
            {
                importer.forceToMono = true;
                changed = true;
            }

            // Get current settings
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            // Compare + assign only if different
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

            if (!Mathf.Approximately(settings.quality, 0.25f))
            {
                settings.quality = 0.25f;
                changed = true;
            }

            if (settings.sampleRateSetting != AudioSampleRateSetting.OverrideSampleRate)
            {
                settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                changed = true;
            }

            if (settings.sampleRateOverride != 22050)
            {
                settings.sampleRateOverride = 22050;
                changed = true;
            }

            // Apply only if something changed
            if (changed)
            {
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
                changedCount++;
            }
        }

        Debug.Log($"Optimized {changedCount} unique voice clips on {name}.");
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
    }
}
#endif