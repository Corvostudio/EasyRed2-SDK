using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class AttachmentScope : Attachment
{
    public Transform aimPos;
    [Range(20, 65)]
    public float aimingFOV = 30;
    [Tooltip("Increased AI night vision when using this scope")]
    [Range(0, 1)]
    public float minimumNightVision = 0;

    [Header("Scope data")]
    public ScopeCameraManager scopeCameraManager = new ScopeCameraManager();

    //deprecated
    [HideInInspector] public GameObject enableOnFPS; // old but still required for deprecated mods


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (enableOnFPS != null
            && scopeCameraManager != null
            && scopeCameraManager.enableOnFPS == null) // only when target empty
        {
            scopeCameraManager.enableOnFPS = enableOnFPS;
            UnityEditor.EditorUtility.SetDirty(this); // so it persists to disk on next save
        }
    }
#endif
}