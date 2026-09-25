using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AttachmentBayonet : Attachment
{
    [Header("Integrated bayonet (only when referenced by GenericGun.integratedBayonet)")]
    [Tooltip("The bayonet starts folded and can be mounted / folded back from the weapon's quick actions and the inventory (the AI mounts it before charging). Off = permanently mounted, the pose in the prefab is the mounted one.")]
    public bool foldable = false;
    [Tooltip("Local position when mounted. The folded (rest) pose is the one authored in the prefab.")]
    public Vector3 mountedLocalPosition;
    [Tooltip("Local rotation (euler angles) when mounted.")]
    public Vector3 mountedLocalRotation;
    [Tooltip("Seconds to move between the two poses. 0 = jumps straight to the other pose.")]
    public float foldSeconds = .35f;
}
