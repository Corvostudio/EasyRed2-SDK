using UnityEngine;

/// <summary>
/// Launched look of a grenade: fired from a launcher, each listed renderer gets the given mesh and/or material (the
/// Type 91 with the propellant charge screwed on its base, in place of the bare hand grenade). List every renderer that
/// shows the grenade, LOD levels included. Thrown by hand, the grenade keeps the prefab look. Put it on the grenade root.
/// </summary>
public partial class GrenadeLaunchMeshSwap : GrenadeLaunchModifier
{
    [System.Serializable]
    public class Swap
    {
        [Tooltip("Renderer of the grenade prefab to change")]
        public Renderer renderer;
        [Tooltip("Mesh of the launched round, set on the MeshFilter of that renderer. Empty = mesh unchanged")]
        public Mesh mesh;
        [Tooltip("Material of the launched round. Empty = material unchanged")]
        public Material material;
    }
    public Swap[] swaps = new Swap[0];
}
