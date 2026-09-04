using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

public partial class AmmoBeltsFPSManager : MonoBehaviour
{
    public FPSMagManager[] compatibleMagazines;
}

[System.Serializable]
public class FPSMagManager
{
    public string mag_id;
    public GameObject[] enableOnSetMag;
    public GameObject[] individualBullets;
    public string override_reload_anim_partial;
    public string override_reload_anim_full;


    public void Show(bool show)
    {
        foreach (GameObject go in enableOnSetMag)
            go.SetActive(show);
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(AmmoBeltsFPSManager))]
public class AmmoBeltsFPSManagerEditor : Editor
{
    private void OnEnable()
    {
        EditorSceneManager.sceneSaving += OnSceneSaving;
        PrefabStage.prefabSaving += OnPrefabSaving;
    }

    private void OnDisable()
    {
        EditorSceneManager.sceneSaving -= OnSceneSaving;
        PrefabStage.prefabSaving -= OnPrefabSaving;
    }

    private void OnSceneSaving(Scene scene, string path) => ResetAllBullets();
    private void OnPrefabSaving(GameObject prefab) => ResetAllBullets();

    private void ResetAllBullets()
    {
        AmmoBeltsFPSManager manager = target as AmmoBeltsFPSManager;
        if (manager == null || manager.compatibleMagazines == null) return;

        foreach (FPSMagManager mag in manager.compatibleMagazines)
        {
            if (mag == null || mag.individualBullets == null) continue;
            foreach (GameObject bullet in mag.individualBullets)
            {
                if (bullet != null && !bullet.activeSelf)
                {
                    bullet.SetActive(true);
                    EditorUtility.SetDirty(bullet);
                }
            }
        }
    }

    public override void OnInspectorGUI()
    {
        AmmoBeltsFPSManager manager = (AmmoBeltsFPSManager)target;
        if (!manager.GetComponent<AutomaticGunWithAmmoBelt>())
        {
            EditorGUILayout.HelpBox("Missing or unlinked AutomaticGunWithAmmoBelt!",MessageType.Error);
        }


        DrawDefaultInspector();

        if (manager.compatibleMagazines == null || manager.compatibleMagazines.Length == 0)
            return;

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Bullet Count Preview (editor only)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Bullets are reset to all-active on save / validate.", MessageType.Info);

        foreach (FPSMagManager mag in manager.compatibleMagazines)
        {
            if (mag == null || mag.individualBullets == null || mag.individualBullets.Length == 0)
                continue;

            int max = mag.individualBullets.Length;
            int currentCount = CountActive(mag);
            string label = string.IsNullOrEmpty(mag.mag_id) ? "Bullets" : mag.mag_id;

            EditorGUI.BeginChangeCheck();
            int newCount = EditorGUILayout.IntSlider(label, currentCount, 0, max);
            if (EditorGUI.EndChangeCheck() && newCount != currentCount)
            {
                for (int i = 0; i < max; i++)
                {
                    GameObject bullet = mag.individualBullets[i];
                    if (bullet == null) continue;
                    Undo.RecordObject(bullet, "Update Bullet Count");
                    bullet.SetActive(i < newCount);
                    EditorUtility.SetDirty(bullet);
                }
            }
        }
    }

    private int CountActive(FPSMagManager mag)
    {
        int count = 0;
        foreach (GameObject b in mag.individualBullets)
            if (b != null && b.activeSelf) count++;
        return count;
    }
}
#endif