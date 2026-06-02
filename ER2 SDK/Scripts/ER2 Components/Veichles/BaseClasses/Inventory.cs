using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public partial class Inventory
{
    [Tooltip("Defines the number of primary weapons present inside the vehicles")]
    [Range(0, 100)]
    public int maxPrimary = 1;//Soldati aggiornano questo valore aumentandolo per esempio se hanno delle borse porta armi
    [Tooltip("Defines the number of secondary weapons present inside the vehicles")]
    [Range(0, 100)]
    public int maxSecondary = 1;//Soldati aggiornano questo valore basato anche sulle fondine che hanno
    [Tooltip("Defines the max weight carriable by the vehicle")]
    [Range(1, 10000)]
    public int maxWeight = 30;//Soldati aggiornano questo valore basato anche sulle borse che hanno

    public List<VirtualItem> items = new List<VirtualItem>();
}
    
[System.Serializable]
public partial class VirtualItem
{
    public string item_id;
}


