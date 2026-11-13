using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class ItemObject : Interagible
{
    [Tooltip("Weight of the item in the inventory, in kg.")]
    [Range(0, 100)]
    public float mass = 3.0f;

    //[HideInInspector]
    [Tooltip("Id of the item (automatically generated)")]
    public string item_id = "Unknown_id";


    [Tooltip("Icon of the item.")]
    public Sprite icon;


}
