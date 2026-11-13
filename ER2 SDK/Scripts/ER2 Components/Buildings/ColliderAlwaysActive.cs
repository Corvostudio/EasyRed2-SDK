using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ColliderAlwaysActive : MonoBehaviour
{
    [Tooltip("Keeps this collider active at any distance and makes the prop spawn up to 4 km away. Heavy on performance: use only where needed.")]
    public ColliderAlwaysActiveType rendAndCollDistance = ColliderAlwaysActiveType.upTo4km;
    public enum ColliderAlwaysActiveType 
    {
        //upTo1km = 1000, TBA
        upTo4km = 4000
    }
}
