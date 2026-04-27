using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AudioVolumeFixer: Editor
{
    public static float volumeMultiplier = 15f; // Adjust this value as needed
    public static string audioFolderPath; // Set this in the Inspector


    [MenuItem("ER2 TOOLS/Tools/Fix audio")]
    public static void FixSounds()
    {
        string audioFolderPath = EditorUtility.OpenFolderPanel("Select Folder with Audio Clips", "", "");
        if (string.IsNullOrEmpty(audioFolderPath))
        {
            Debug.LogWarning("No folder selected.");
            return;
        }
        string[] audioFiles = Directory.GetFiles(audioFolderPath, "*.wav");
        
        foreach (string filePath in audioFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            AudioClip originalClip = LoadAudioClipFromFile(filePath);

            Debug.Log(originalClip);
            if (originalClip != null)
            {
                AudioClip adjustedClip = AdjustVolume(originalClip);
                SaveAdjustedClip(adjustedClip, fileName);
            }
        }
    }

    private static AudioClip LoadAudioClipFromFile(string filePath)
    {
        filePath = filePath.Substring(Application.dataPath.Length-6);
        Debug.Log(filePath);
        return (AudioClip)AssetDatabase.LoadAssetAtPath(filePath, typeof(AudioClip));
        //return Resources.Load<AudioClip>(Path.GetFileNameWithoutExtension(filePath));
    }

    private static AudioClip AdjustVolume(AudioClip originalClip)
    {
        AudioSource tempAudioSource = new GameObject("Adio managaer").AddComponent<AudioSource>();
        tempAudioSource.clip = originalClip;
        tempAudioSource.volume = volumeMultiplier;

        AudioClip adjustedClip = AudioClip.Create(
            originalClip.name + "_Adjusted",
            tempAudioSource.clip.samples,
            tempAudioSource.clip.channels,
            tempAudioSource.clip.frequency,
            false
        );

        float[] data = new float[tempAudioSource.clip.samples * tempAudioSource.clip.channels];
        tempAudioSource.GetOutputData(data, 0);
        adjustedClip.SetData(data, 0);

        Debug.Log("Fixing " + originalClip.name);
        Destroy(tempAudioSource.gameObject);

        return adjustedClip;
    }

    private static void SaveAdjustedClip(AudioClip adjustedClip, string fileName)
    {
        string outputPath = Path.Combine(Application.dataPath, "AdjustedAudio", fileName + "_Adjusted.wav");
        SavWav.Save(ref outputPath, adjustedClip);
        Debug.Log("AudioClip " + fileName + " adjusted and saved to: " + outputPath);
    }
}