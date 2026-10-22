using UnityEngine;
/// <summary>
/// A melee move set shared between weapons (the knife's, the katana's...): assigned to a MeleeWeapon or to a bayonet it
/// takes the place of their inline move set.
/// </summary>
[CreateAssetMenu(fileName = "MeleeMoveSet", menuName = "Weapons/Melee Move Set")]
public class MeleeMoveSetData : ScriptableObject
{
    public MeleeMoveSet moveSet = new MeleeMoveSet();
}
