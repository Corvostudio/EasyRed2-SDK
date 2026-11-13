using UnityEngine;


public partial class InventoryManager : MonoBehaviour
{
    [Tooltip("Distance, in metres, from which the player can open this inventory.")]
    [Range(0, 15)]
    public float inventoryInteractionDistance = 2.3f;
    public Inventory inventory;
    [Tooltip("The gameobject transform position of the inventory")]
    public Transform inventoryPos = null;

    
}
