using UnityEngine;

public partial class Vehicle : MonoBehaviour, Damagable
{
    [HideInInspector]public int param_version = 10;

    [Header("General")]
    [Tooltip("Hit points of the vehicle body (hull). When they run out the vehicle is destroyed.")]
    [Range(0, 10000)]
    public short life = 700;
    [Tooltip("Icon of the vehicle, shown in the selection menus and in the what's new panel.")]
    public Sprite icon;

    [Header("Crew")]
    [Tooltip("Seats of the vehicle: where units sit, which seat drives and which turret each one operates.")]
    public Seats[] seats;
    [Tooltip("On: the player steers from any seat. Off: only from the driver's seat (from the others an AI driver drives on his own).")]
    public bool playerCanDriveFromAnySeat = true;
    [Tooltip("Dummy crew figures on the vehicle: they get the uniform, gear and helmet of the vehicle's faction.")]
    public AutoTransportDriverUniform[] autoTransportDrivers = new AutoTransportDriverUniform[0];

    [Header("Movement and camera")]
    [Tooltip("Turn rate. Planes: pitch, yaw and roll speed. Wheeled and tracked vehicles turn with their wheels: for them it only tunes the AI and the network smoothing.")]
    [Range(.1f, 3)]
    public float rotationSpeed = .3f;//only if he admit driver
    [Tooltip("Third-person camera distance multiplier: 1 suits a small vehicle. The player's camera distance setting is applied on top.")]
    [Range(.5f, 30)]
    public float tpsCamDist = 1;

    [Header("Destruction")]
    [Tooltip("Parts that fly off when the fuel tank blows up (typically the turret). Fire and explosion sound are placed on the first one.")]
    public GameObject[] loosePartsOnInternalExplosion = new GameObject[0];
}

[System.Serializable]
public partial class Seats
{
    [Tooltip("Vehicle Seat component where the unit sits.")]
    public VehicleSeat seat;
    [Tooltip("Turret operated from this seat (optional).")]
    public Turret connectedTurret;
    [Tooltip("The unit in this seat drives the vehicle.")]
    public bool isDriver = false;
    [Tooltip("Hide the unit's hand-held weapon while he sits here. The driver's seat and seats with a turret always hide it.")]
    public bool forceHideWeaponWhenHere = false;
    [Tooltip("Maximum left/right rotation of the camera in FPS on this seat")]
    [Range(0, 180)]
    public float maxLookY = 60;

    [Tooltip("Custom IK: hands on the two IK targets below. Current Weapon: hands keep holding the unit's own weapon.")]
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