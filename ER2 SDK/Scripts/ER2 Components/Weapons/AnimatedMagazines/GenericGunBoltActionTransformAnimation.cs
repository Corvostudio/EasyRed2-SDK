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

    [Tooltip("Firing-pin / striker mesh. On trigger pull it lerps to the captured 'fired' pose; when the bolt is cycled or the weapon is reloaded it lerps back to the captured 'cocked' pose (after a short delay, to line up with the bolt opening). Capture both poses with the buttons below. Leave empty to disable.")]
    public Transform percussor;

    // Captured poses (local space). Set via the inspector buttons.
    [HideInInspector] [SerializeField] private Vector3 percussorCockedLocalPosition;
    [HideInInspector] [SerializeField] private Quaternion percussorCockedLocalRotation = Quaternion.identity;

    [HideInInspector] [SerializeField] private Vector3 percussorFiredLocalPosition;
    [HideInInspector] [SerializeField] private Quaternion percussorFiredLocalRotation = Quaternion.identity;


    [System.NonSerialized] private TransformPose[] rotateDefaults;
    [System.NonSerialized] private TransformPose[] slideDefaults;
    [System.NonSerialized] private Coroutine routine;

    // Firing-pin runtime state.
    [System.NonSerialized] private Coroutine pinRoutine;


    private struct TransformPose
    {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }
}
