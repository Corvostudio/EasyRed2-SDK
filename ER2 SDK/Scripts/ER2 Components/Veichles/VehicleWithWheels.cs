using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class VehicleWithWheels : MovableVehicle
{
    public enum OnEnemyEncounter
    {
        nothing,
        ejectPassengers,
        ejectAll
    }

    [Header("AI")]
    [Tooltip("Attack Vehicle: advances on the objective. Self Propelled Artillery: keeps its distance and answers radio fire requests.")]
    public WheeledVehicleAi aiType = WheeledVehicleAi.attackVehicle;
    [Tooltip("AI crew reaction to the enemy or to heavy damage: stay in, eject the passengers, or eject everyone. Any value but Nothing also marks the vehicle as a transport.")]
    public OnEnemyEncounter onEnemyEncounter = OnEnemyEncounter.nothing;
    public enum WheeledVehicleAi { attackVehicle = 0, selfPropelledArtillery = 1 }

    [Header("Engine and wheels")]
    [Tooltip("Motor torque sent to the wheels at full throttle.")]
    [Range(50, 5000)]
    public float maxMotorTorque = 800;
    [Tooltip("How quickly the motor torque builds up: this value x 200 per second forward, x 30 per second in reverse.")]
    [Range(1, 200)]
    public float accelerationSpeed = 50;
    [Tooltip("Wheel colliders on the left side of the vehicle (wheels or track rollers).")]
    public WheelCollider[] leftTrack;
    [Tooltip("Wheel colliders on the right side of the vehicle (wheels or track rollers).")]
    public WheelCollider[] rightTrack;

    [Header("Sounds")]
    [Tooltip("Sound played when the vehicle crashes into something.")]
    public AudioClip crashSound;

    [HideInInspector]//deprecated: not read by the game, the centre of mass is computed from the wheels
    public Vector3 centerOfMass = new Vector3(0, 1, 0);

}


#if UNITY_EDITOR

[CustomEditor(typeof(VehicleWithWheels), true)] // 'true' => vale anche per VehicleTank
[CanEditMultipleObjects]
public class VehicleWithWheelsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        var report = new List<string>();
        bool anyMismatch = false;
        int vehiclesChecked = 0;

        foreach (var obj in targets)
        {
            var vehicle = (VehicleWithWheels)obj;
            var wheels = GatherWheels(vehicle);
            if (wheels.Count < 2) continue;
            vehiclesChecked++;

            var reference = wheels[0];
            var mismatched = GetMismatched(wheels, reference);
            if (mismatched.Count > 0)
            {
                anyMismatch = true;
                report.Add($"[{vehicle.name}] ref: {reference.name}");
                foreach (var m in mismatched)
                    report.Add("    " + m);
            }
        }

        if (vehiclesChecked == 0)
            return;

        if (!anyMismatch)
        {
            /*EditorGUILayout.HelpBox(
                targets.Length > 1
                    ? $"All {vehiclesChecked} selected vehicles have identical friction curves across their wheels."
                    : "All wheels share identical friction curves.",
                MessageType.Info);*/
            return;
        }

        EditorGUILayout.HelpBox(
            "Wheel friction curves are NOT identical. Asymmetric forward friction makes the vehicle yaw under braking.\n\n" +
            string.Join("\n", report),
            MessageType.Warning);

        if (GUILayout.Button(targets.Length > 1
                ? "Fix all selected: copy each vehicle's first-wheel friction to its wheels"
                : "Fix: copy first-wheel friction to all wheels"))
        {
            FixAll();
        }
    }

    void FixAll()
    {
        var allWheels = new List<Object>(); // tutte le ruote toccate, per un solo Undo
        var perVehicle = new List<(List<WheelCollider> wheels, WheelCollider reference)>();

        foreach (var obj in targets)
        {
            var vehicle = (VehicleWithWheels)obj;
            var wheels = GatherWheels(vehicle);
            if (wheels.Count < 2) continue;
            perVehicle.Add((wheels, wheels[0]));
            foreach (var w in wheels) allWheels.Add(w);
        }

        if (allWheels.Count == 0) return;

        Undo.RecordObjects(allWheels.ToArray(), "Sync wheel friction");

        foreach (var (wheels, reference) in perVehicle)
        {
            var fwd = reference.forwardFriction;
            var side = reference.sidewaysFriction;
            foreach (var w in wheels)
            {
                w.forwardFriction = fwd;
                w.sidewaysFriction = side;
                EditorUtility.SetDirty(w);
                if (PrefabUtility.IsPartOfPrefabInstance(w))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(w);
            }
        }
    }

    static List<WheelCollider> GatherWheels(VehicleWithWheels vehicle)
    {
        var wheels = new List<WheelCollider>();
        if (vehicle.rightTrack != null)
            foreach (var w in vehicle.rightTrack) if (w) wheels.Add(w);
        if (vehicle.leftTrack != null)
            foreach (var w in vehicle.leftTrack) if (w) wheels.Add(w);
        return wheels;
    }

    static List<string> GetMismatched(List<WheelCollider> wheels, WheelCollider reference)
    {
        var mismatched = new List<string>();
        for (int i = 1; i < wheels.Count; i++)
        {
            bool fwdOk = CurvesEqual(wheels[i].forwardFriction, reference.forwardFriction);
            bool sideOk = CurvesEqual(wheels[i].sidewaysFriction, reference.sidewaysFriction);
            if (!fwdOk || !sideOk)
            {
                string which = (!fwdOk && !sideOk) ? "forward + sideways"
                             : (!fwdOk ? "forward" : "sideways");
                mismatched.Add($"• {wheels[i].name} ({which})");
            }
        }
        return mismatched;
    }

    static bool CurvesEqual(WheelFrictionCurve a, WheelFrictionCurve b)
    {
        return Mathf.Approximately(a.extremumSlip, b.extremumSlip)
            && Mathf.Approximately(a.extremumValue, b.extremumValue)
            && Mathf.Approximately(a.asymptoteSlip, b.asymptoteSlip)
            && Mathf.Approximately(a.asymptoteValue, b.asymptoteValue)
            && Mathf.Approximately(a.stiffness, b.stiffness);
    }
}
#endif