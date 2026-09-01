using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Full animation/sound override set that activates when a specific attachment is installed on the weapon.
/// Empty fields fall back to the weapon base AnimationData, so partial overrides are fine.
/// Example: MG42 + bipod -> the weapon rests on the bipod, so equip/reload/barrel-change/deploy all change.
/// </summary>
[System.Serializable]
public class AttachmentAnimationOverride
{
    [Tooltip("Item id of the attachment that activates this override when installed (e.g. the MG42 bipod item id).")]
    public string attachment_id;

    [Tooltip("Animation/sound overrides applied while the attachment is installed. Empty fields fall back to the weapon base AnimationData.")]
    public AnimationData animations;

    [Header("While DEPLOYED (weapon resting on the bipod)")]
    [Tooltip("[Optional] Reload used while the bipod is DEPLOYED. If the needed field is empty the weapon simply lifts off the bipod (undeploy) and reloads normally. (The gun_id field is ignored here.)")]
    public MagazineOverrideAnimation deployedReloadAnimations;
    [Tooltip("[Optional] Barrel change animation while DEPLOYED. Empty = the weapon lifts off the bipod to change the barrel.")]
    public string fps_change_barrell_deployed;
    public AudioClip fps_change_barrell_sound_deployed;

    //nota: gli override per-MAGAZINE (es. MG42+bipode con drum) vivono nell'AmmoBeltsFPSManager
    //(FPSMagManager.bipodOverrides), accanto agli altri dati per-magazine dell'arma
}
