using UnityEngine;

/// <summary>
/// The first person side of an item swung in hand, knife or sword: hands set, equip clip, swing clips, sounds.
/// The defaults are the knife's. The third person controller is set apart: tpsAnims.userController on a melee
/// weapon, knifeTpsController on a bayonet.
/// </summary>
[System.Serializable]
public partial class MeleeMoveSet
{
    [Tooltip("FPS hands set for sway and walk: a key of the FPS animation sets, for example anims_pistol.")]
    public string fpsHandsSet = "anims_knife";
    [Tooltip("FPS clip played taking the item in hand, played backwards to put it away.")]
    public string fpsEquipAnim = "Melee_Bayonet_Equip";
    [Tooltip("FPS swing clips, used in turn. Empty = the baked melee of the hands set.")]
    public string[] fpsSwingAnims = { "Melee_Bayonet_Swing_L", "Melee_Bayonet_Swing_R" };
    [Tooltip("Swing sounds of the er2sounds bundle, one at random.")]
    public string[] swingSounds = { "SwingSharp1H_1", "SwingSharp1H_2" };
}
