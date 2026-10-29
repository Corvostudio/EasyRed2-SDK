using UnityEngine;

/// <summary>
/// Base of the components that change a grenade when it is fired from a launcher (knee mortar, grenade discharger)
/// instead of thrown by hand: the launcher runs every one found on the root of the spawned round, before it leaves the
/// tube. The grenade in the hands and in the inventory is never touched. Derive from it for whatever the launched round
/// needs that the thrown one does not; GrenadeLaunchMeshSwap gives it another mesh or material (the Type 91 with its
/// propellant charge).
/// </summary>
public abstract partial class GrenadeLaunchModifier : MonoBehaviour
{

}
