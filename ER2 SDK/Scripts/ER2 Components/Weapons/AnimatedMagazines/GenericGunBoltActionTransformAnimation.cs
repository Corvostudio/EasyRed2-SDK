using UnityEngine;

public partial class GenericGunBoltActionTransformAnimation : GenericGunActionAnimation
{
    [Header("Rotate open / close")]

    public GameObject[] rotateObjects;
    public Vector3 localRotationAxis = Vector3.forward;
    public float openRotationDegrees = 90f;


    [Header("Slide back / forward")]

    public GameObject[] slideObjects;
    public Vector3 localOpenOffset = new Vector3(0f, 0f, -0.1f);


    [Header("Firing pin")]

    [Tooltip("Firing-pin / striker mesh. On every shot it snaps to the captured 'fired' pose. Its rest pose is simply its authored transform, restored when the bolt is cycled (ResetPose runs at the start of Play/PlayReload). Capture the fired pose with the button in the inspector. Leave empty to disable.")]
    public Transform percussor;

    // Fired pose the pin snaps to on each shot (local space).
    [HideInInspector] [SerializeField] private bool hasPercussorFiredPose;
    [HideInInspector] [SerializeField] private Vector3 percussorFiredLocalPosition;
    [HideInInspector] [SerializeField] private Quaternion percussorFiredLocalRotation = Quaternion.identity;


    [System.NonSerialized] private TransformPose[] rotateDefaults;
    [System.NonSerialized] private TransformPose[] slideDefaults;
    [System.NonSerialized] private Coroutine routine;

    // Rest pose is captured automatically from the pin's authored transform.
    [System.NonSerialized] private bool percussorRestCached;
    [System.NonSerialized] private Vector3 percussorRestRuntimePos;
    [System.NonSerialized] private Quaternion percussorRestRuntimeRot = Quaternion.identity;


    private struct TransformPose
    {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }
}
