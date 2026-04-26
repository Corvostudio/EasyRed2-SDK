using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[ExecuteInEditMode]
public partial class InteragibleDoorAdditionalInteractor : Interagible
{
    public InteragibleDoor connectedDoor;

#if UNITY_EDITOR
    private void Awake()
    {
        if (!connectedDoor)
            connectedDoor = GetComponentInParent<InteragibleDoor>();
    }
#endif
}
