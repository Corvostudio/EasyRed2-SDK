using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class AutomaticGunWithAmmoBelt : GenericGun
{
    [HideInInspector] public AmmoBeltsFPSManager beltManager = null;
}