using UnityEngine;


public partial class InventoryManager : MonoBehaviour
{
    [Tooltip("The distance from which the player can open the inventory from")]
    [Range(0, 15)]
    public float inventoryInteractionDistance = 2.3f;
    public Inventory inventory;
    [Tooltip("The gameobject transform position of the inventory")]
    public Transform inventoryPos = null;

    
}
