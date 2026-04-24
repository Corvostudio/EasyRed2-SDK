using UnityEngine;

public partial class Vehicle : MonoBehaviour, Damagable
{
    [HideInInspector]public int param_version = 10;

    [Range(0, 10000)]
    public short life = 700;

    [Tooltip("icon of the vehicle")]
    public Sprite icon;

    [Tooltip("Seats for the units")]
    public Seats[] seats;


    [Tooltip("Third person camera distance")]
    [Range(.5f, 30)]
    public float tpsCamDist = 1;

    [Tooltip("Speed of rotation of the vehicle")]
    [Range(.1f, 3)]
    public float rotationSpeed = .3f;//only if he admit driver

    [Tooltip("Player automatically control the vehicle from any seat if the vehicle has a driver")]
    public bool playerCanDriveFromAnySeat = true;

    //public AutoTransportDriverUniform[] autoTransportDrivers = new AutoTransportDriverUniform[0];

    [Tooltip("Parts to detach in case of explosion")]
    public GameObject[] loosePartsOnInternalExplosion = new GameObject[0];
}

[System.Serializable]
public partial class Seats
{
    [Tooltip("Connected seat")]
    public VehicleSeat seat;
    [Tooltip("turret controllable by this seat (optional)")]
    public Turret connectedTurret;
    [Tooltip("This is the driver position?")]
    public bool isDriver = false;
    [Tooltip("When unit is set here, don't show his weapon in hands")]
    public bool forceHideWeaponWhenHere = false;
    [Tooltip("Maximum left/right rotation of the camera in FPS on this seat")]
    [Range(0, 180)]
    public float maxLookY = 60;

    [Tooltip("Where the hands should be placed")]
    public SeatHandsPosition seatHandsPositions = SeatHandsPosition.customIK;
    [Tooltip("IK target position of the right hand")]
    public Transform rightHandIK;
    [Tooltip("IK target position of the left hand")]
    public Transform lefthandIsIK;
    [Tooltip("Viewport 3D model of the vehicle to enable and place the camera in when in FPS")]
    public VehViewPort visionPort = null;

    
}
public enum SeatHandsPosition
{
    customIK,
    currentWeapon
}