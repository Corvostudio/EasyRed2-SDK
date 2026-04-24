using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ImpactSoundManager : MonoBehaviour
{

    [Range(1, 2000)]
    public float max_hear_distance = 20;

    public AudioClip impactSound_generic;
    public AudioClip impactSound_concrete;

}
