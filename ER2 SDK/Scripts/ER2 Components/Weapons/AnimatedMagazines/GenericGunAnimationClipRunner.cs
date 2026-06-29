using UnityEngine;

public class GenericGunAnimationClipRunner : GenericGunActionAnimation
{
    public Animation targetAnimation;
    public string animationName = "bolt_anim";
    public bool stopBeforePlay = false;
    public bool rewindBeforePlay = false;

    public override void Play(GenericGun gun, Soldier soldier)
    {
        if (!isActiveAndEnabled || string.IsNullOrEmpty(animationName))
            return;

        if (!targetAnimation)
            targetAnimation = GetComponent<Animation>();

        if (!targetAnimation && gun)
            targetAnimation = gun.GetComponent<Animation>();

        if (!targetAnimation)
            return;

        if (stopBeforePlay)
            targetAnimation.Stop();

        if (rewindBeforePlay)
            targetAnimation.Rewind(animationName);

        targetAnimation.Play(animationName);
    }
}