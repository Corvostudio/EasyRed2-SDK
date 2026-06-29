using UnityEngine;

public partial class GenericGunAnimationClipRunner : GenericGunActionAnimation
{
    public Animation targetAnimation;
    public string animationName = "bolt_anim";
    public bool stopBeforePlay = false;
    public bool rewindBeforePlay = false;

}