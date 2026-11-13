using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class InventoryManagerFiller : MonoBehaviour
{
    [Tooltip("IDs of the items put in the inventory when the vehicle spawns.")]
    public string[] addToInventory=new string[0];
}
