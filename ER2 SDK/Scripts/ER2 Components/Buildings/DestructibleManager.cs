using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode]
#endif
public partial class DestructibleManager : MapPropIndexable// MapPropIndexable
{
    [Tooltip("All the individual parts of the building that can recieve damage. Requires DestructableBuilding components")]
    public DestructableBuilding[] destructibleParts = new DestructableBuilding[0];

    //props management
    [HideInInspector] public BuildingFurnitureSpawner[] fornitureSpawners = new BuildingFurnitureSpawner[0];

    //doors management
    [HideInInspector] public InteragibleDoor[] registeredDoors = new InteragibleDoor[0];


#if UNITY_EDITOR
    private void Awake()
    {
        if (!Application.isPlaying)
        {
            CaptureFurnitureSpawners();
            AcquireDestructibleParts();
            CaptureDoors();
        }
    }


    public void AcquireDestructibleParts()
    {
        destructibleParts = GetComponentsInChildren<DestructableBuilding>();
    }
    public void CaptureFurnitureSpawners()
    {
        fornitureSpawners = gameObject.GetComponentsInChildren<BuildingFurnitureSpawner>(true);
    }
    public void CaptureDoors()
    {
        registeredDoors = GetComponentsInChildren<InteragibleDoor>();
        if (TryGetComponent<DoorRegister>(out var reg))
            DestroyImmediate(reg);
    }
#endif
}
