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
    [Tooltip("FPS clip played taking the item in hand.")]
    public string fpsEquipAnim = "Melee_Bayonet_Equip";
    [Tooltip("FPS clip played putting the item away. Empty = the equip clip backwards.")]
    public string fpsUnequipAnim = "Melee_Bayonet_Unequip";
    [Tooltip("FPS swing clips, used in turn. Empty = the baked melee of the hands set.")]
    public string[] fpsSwingAnims = { "Melee_Bayonet_Swing_L", "Melee_Bayonet_Swing_R" };
    [Tooltip("Swing sounds of the er2sounds bundle, one at random.")]
    public string[] swingSounds = { "SwingSharp1H_1", "SwingSharp1H_2" };
    [Tooltip("FPS loop while sprinting with the item in hands. Empty = the procedural sprint pose.")]
    public string fpsSprintAnim = "Melee_Sprint";
    [Tooltip("FPS block (aim button): into the stance, held on its last frame; out of it, back to the hold pose; the recoil of a parried hit.")]
    public string fpsBlockInAnim = "Melee_Block_In";
    public string fpsBlockOutAnim = "Melee_Block_Out";
    public string fpsBlockRecoilAnim = "Melee_Block_Recoil";
    [Tooltip("Frames before the end of a swing within which the next swing can already start.")]
    public int swingInterruptFrames = 6;
    [Tooltip("Frames skipped at the start of a swing chained early.")]
    public int swingSkipFrames = 3;
}
