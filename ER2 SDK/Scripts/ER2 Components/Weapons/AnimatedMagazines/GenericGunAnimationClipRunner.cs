using UnityEngine;

public partial class GenericGunAnimationClipRunner : GenericGunActionAnimation
{
    [Tooltip("Animation component that plays the clips. Empty = the one on this object, then the one on the weapon.")]
    public Animation targetAnimation;
    [Tooltip("Clip played when the bolt or action of the weapon is cycled.")]
    public string animationName = "bolt_anim";
    [Tooltip("Stop the Animation component before playing the clip.")]
    public bool stopBeforePlay = false;
    [Tooltip("Rewind the clip before playing it.")]
    public bool rewindBeforePlay = false;

    [Header("Fire clip (replaces fire_anim / fire_anim_lastBullet)")]

    [Tooltip("Clip played at every shot. Replaces the Fire Anim of the weapon while this component is assigned to it. Empty = unused.")]
    public string fireAnimationName = "";

    [Tooltip("Clip played by the shot that empties the weapon (e.g. the Luger toggle staying open). Empty = the fire clip above.")]
    public string fireLastBulletAnimationName = "";

}
