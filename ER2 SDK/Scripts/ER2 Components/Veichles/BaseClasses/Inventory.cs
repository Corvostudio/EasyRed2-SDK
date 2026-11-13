using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public partial class Inventory
{
    [HideInInspector]//deprecated: not read by the game
    public int maxPrimary = 1;//Soldati aggiornano questo valore aumentandolo per esempio se hanno delle borse porta armi
    [HideInInspector]//deprecated: not read by the game
    public int maxSecondary = 1;//Soldati aggiornano questo valore basato anche sulle fondine che hanno
    [Tooltip("Maximum total weight of the items this inventory can hold.")]
    [Range(1, 10000)]
    public int maxWeight = 30;//Soldati aggiornano questo valore basato anche sulle borse che hanno

    public List<VirtualItem> items = new List<VirtualItem>();
}
    
[System.Serializable]
public partial class VirtualItem
{
    public string item_id;
}


