using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;


public partial class AmmoRefillCrate : Interagible
{
    [Tooltip("The delay time between refills")]
    [Range(0, 300)]
    public float delay = 30;
    [Tooltip("The icon shown over the crate")]
    public Sprite icon;

    [Tooltip("Defines the object inside the ammo refill crate")]
    public string[] overrideContent;
}
