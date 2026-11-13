using UnityEngine;


public partial class FloatingObject : MonoBehaviour
{
    [Header("Buoyancy")]
    [Tooltip("Height of the waterline above the pivot, in metres.")]
    [Range(-10, 10)] public float floatingHeight = 1.2f;
    [Tooltip("Length of the outline drawn in the scene view. Not used by the physics.")]
    [Range(0, 50)] public float objectLenght = 4;
    [Tooltip("Width of the outline drawn in the scene view. Not used by the physics.")]
    [Range(0, 50)] public float objectWidth = 2.5f;

    [System.NonSerialized] private float halfLength;
    [System.NonSerialized] private float halfWidth;

    [Header("Driving on water")]
    [Tooltip("Acceleration on water, in m/s2.")]
    public float waterAcceleration = 2f;
    [Tooltip("Top speed on water, in m/s.")]
    public float waterMaxSpeed = 5f;
    [Tooltip("Turn rate on water, in degrees per second.")]
    public float waterTurnSpeed = 30f;
    [Tooltip("Linear damping of the rigidbody while in water.")]
    public float waterDrag = 0.8f;
    [Tooltip("Angular damping of the rigidbody while in water.")]
    public float waterAngularDrag = 1f;

    [HideInInspector]//deprecated: not read by the game, the waves are fixed in code
    [Range(0.01f, 3)] public float wavesFrequency = .5f;
    [HideInInspector]//deprecated: not read by the game, the waves are fixed in code
    [Range(0.0f, 1)] public float wavesPower = .01f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        float hl = (Application.isPlaying) ? halfLength : objectLenght * 0.5f;
        float hw = (Application.isPlaying) ? halfWidth : objectWidth * 0.5f;

        Vector3 heightOffset = transform.up * floatingHeight;
        Vector3 p1 = transform.position + transform.forward * hl + transform.right * hw + heightOffset;
        Vector3 p2 = transform.position - transform.forward * hl + transform.right * hw + heightOffset;
        Vector3 p3 = transform.position + transform.forward * hl - transform.right * hw + heightOffset;
        Vector3 p4 = transform.position - transform.forward * hl - transform.right * hw + heightOffset;

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p2, p4);
        Gizmos.DrawLine(p1, p3);
    }
}
