using UnityEngine;


public partial class FloatingObject : MonoBehaviour
{
    [Header("Buoyancy Settings")]
    [Range(-10, 10)] public float floatingHeight = 1.2f;
    [Range(0, 50)] public float objectLenght = 4;
    [Range(0, 50)] public float objectWidth = 2.5f;

    [System.NonSerialized] private float halfLength;
    [System.NonSerialized] private float halfWidth;

    [Header("Water Driving Settings")]
    public float waterAcceleration = 2f;
    public float waterMaxSpeed = 5f;
    public float waterTurnSpeed = 30f;
    public float waterDrag = 0.8f;
    public float waterAngularDrag = 1f;

    [Header("Water Waves Settings")]
    [Range(0.01f, 3)] public float wavesFrequency = .5f;
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
