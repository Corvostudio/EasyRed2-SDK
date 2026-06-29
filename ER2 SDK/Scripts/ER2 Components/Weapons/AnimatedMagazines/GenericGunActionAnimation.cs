using UnityEngine;


public abstract class GenericGunActionAnimation : MonoBehaviour
{
    public abstract void Play(GenericGun gun, Soldier soldier);

    public virtual void StopAndReset(GenericGun gun)
    {
        StopAllCoroutines();
    }

    protected virtual void OnDisable()
    {
        StopAllCoroutines();
    }
    public virtual void PlayReload(GenericGun gun, Soldier soldier)
    {

    }
}