using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;


public partial class AmmoRefillCrate : Interagible
{
    [Tooltip("Seconds before the crate can be used again.")]
    [Range(0, 300)]
    public float delay = 30;
    [Tooltip("Icon of the crate in the interaction menu.")]
    public Sprite icon;

    [Tooltip("IDs of the items given instead of ammo. Empty = refills the ammo of the weapons carried.")]
    public string[] overrideContent;
}
