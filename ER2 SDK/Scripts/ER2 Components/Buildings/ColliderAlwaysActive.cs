using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ColliderAlwaysActive : MonoBehaviour
{
    [Header("This script will make the connected collider\nalways active at any distance, and wil increase\nthe despawn distace of this props.\nMake sure to use this if really necessary\nas it has big impact on the performance.\n")]
    public ColliderAlwaysActiveType rendAndCollDistance = ColliderAlwaysActiveType.upTo4km;
    public enum ColliderAlwaysActiveType { upTo4km=4000 }
}
