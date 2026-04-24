using System;
using System.Collections.Generic;
using UnityEngine;
public partial class ItemObjectStackable : ItemObject
{
    [Tooltip("Defines the maximum number of this item that can be carried at once")]
    [Range(0, 250)]
    public int maxStack = 1;
    [Tooltip("Defines the number of this item that can be carried on initialize")]
    [Range(0, 250)]
    public int stackCount = 1;
}