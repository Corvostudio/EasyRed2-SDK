using System;
using System.Collections.Generic;
using UnityEngine;
public partial class ItemObjectStackable : ItemObject
{
    [Tooltip("Maximum number of units in one stack.")]
    [Range(0, 250)]
    public int maxStack = 1;
    [Tooltip("Units in the stack when the item is created.")]
    [Range(0, 250)]
    public int stackCount = 1;
}