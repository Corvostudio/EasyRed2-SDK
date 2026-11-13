using UnityEngine;

/// <summary>
/// Attachment part that slides when the weapon fires, e.g. a scope that is not locked to the receiver (Springfield
/// Unertl) and creeps forward under recoil. On every shot the object moves to localFirePosition and stays there until
/// the weapon plays the animation that brings it back (bolt action or reload, e.g. an FPS bolt action override authored
/// with the scope), or until the weapon is holstered. Put it on the attachment prefab.
/// </summary>
public partial class AttachmentFireTransformAnimation : MonoBehaviour
{
    [Tooltip("Object that slides on each shot. Empty = this attachment root.")]
    public Transform slideObject;

    [Tooltip("Local position of the object at the end of the slide, where a shot leaves it. Match it to the first frame of the animation that pulls it back.")]
    public Vector3 localFirePosition;

    [Tooltip("Slide duration in seconds. 0 = instant.")]
    [Range(0f, .5f)] public float duration = .06f;

    [Tooltip("Slide only when the local player fires in first person, where the pull-back animation exists. Off = for every shooter.")]
    public bool fpsOnly = true;
}
