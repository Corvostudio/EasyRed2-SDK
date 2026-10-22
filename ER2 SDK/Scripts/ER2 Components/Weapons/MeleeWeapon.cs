using UnityEngine;

/// <summary>
/// A weapon held in hand and swung: sword, katana, club. It lives in the inventory and comes out with the melee
/// button held or from the quick menu, like a bayonet used as a knife; the fire button swings it.
/// Third person animations come from tpsAnims.userController (CCV2_knife is the one-handed knife set).
/// Register the prefab among the items (PropType.items).
/// </summary>
public partial class MeleeWeapon : HandheldItem
{
    [Header("Melee weapon")]
    [Tooltip("Damage of one hit. A bayonet does 250.")]
    public float damage = 250;
    [Tooltip("Reach of the swing from the face, in metres. A bayonet held as a knife reaches 0.73, a sword a bit more.")]
    public float reach = 0.73f;
    [Tooltip("Shared move set. Assigned, the inline move set below is ignored.")]
    public MeleeMoveSetData moveSetData;
    [Tooltip("First person clips and sounds. The defaults are the knife's.")]
    public MeleeMoveSet moveSet = new MeleeMoveSet();
}
