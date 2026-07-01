using UnityEngine;

public partial class GenericGunAnimationClipRunner : GenericGunActionAnimation
{
    public Animation targetAnimation;
    public string animationName = "bolt_anim";
    public bool stopBeforePlay = false;
    public bool rewindBeforePlay = false;

    [Header("Fire clip (replaces fire_anim / fire_anim_lastBullet)")]

    [Tooltip("Animation clip played once per shot on the weapon's Animation component. Replaces GenericGun.fire_anim when this action animation is assigned. Leave empty if unused.")]
    public string fireAnimationName = "";

    [Tooltip("Animation clip played specifically on the shot that empties the chamber (e.g. Luger toggle-lock open). Replaces GenericGun.fire_anim_lastBullet. Leave empty to fall back to fireAnimationName.")]
    public string fireLastBulletAnimationName = "";

}
