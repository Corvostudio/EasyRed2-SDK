using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class AutomaticGunWithAmmoBelt : GenericGun
{
    [System.NonSerialized] public AmmoBeltsFPSManager beltManager = null;
}