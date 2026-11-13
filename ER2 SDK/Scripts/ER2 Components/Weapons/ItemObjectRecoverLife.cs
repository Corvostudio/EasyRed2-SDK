using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ItemObjectRecoverLife : ItemObjectStackable
{
    [Header("Life recover properties")]
    [Tooltip("Life restored when used. A soldier has 100.")]
    [Range(0, 100)]
    public int recoverLife = 20;
    [Tooltip("Food rather than medicine: eating it also counts for the food achievement.")]
    public bool isFood = false;
}
