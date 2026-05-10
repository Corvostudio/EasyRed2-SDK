using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class MagnifierGenerator : MonoBehaviour
{
    [Header("Scope sprite image")]
    [Tooltip("Set as sprite on import settings")]
    public Texture scopeSprite;

    [Header("Don't edit below")]
    public MeshRenderer glass;
    //public new Camera camera;
}
