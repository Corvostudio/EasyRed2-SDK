using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ItemObjectRecoverLife : ItemObjectStackable
{
    [Header("Life recover properties")]
    [Tooltip("How much life to restore")]
    [Range(0, 100)]
    public int recoverLife = 20;
    [Tooltip("Is this food or medicine?")]
    public bool isFood = false;
}
