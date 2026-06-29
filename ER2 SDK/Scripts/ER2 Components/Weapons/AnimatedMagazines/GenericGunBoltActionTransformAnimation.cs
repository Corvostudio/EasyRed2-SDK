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


    [System.NonSerialized] private TransformPose[] rotateDefaults;
    [System.NonSerialized] private TransformPose[] slideDefaults;
    [System.NonSerialized] private Coroutine routine;


    private struct TransformPose
    {
        public Transform transform;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }
}