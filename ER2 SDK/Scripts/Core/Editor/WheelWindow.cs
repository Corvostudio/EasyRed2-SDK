#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WheelWindow : EditorWindow
{
    private static Renderer leftTrack, rightTrack;
    private static List<VirtualWheel> wheelColliders = new List<VirtualWheel>();
    private static Vehicle vehicle;
    static string[] side_types = new string[2] { "Left","Right" };
    static string[] follow_types = new string[3] { "Rot, Steer and Spring", "Only Rot", "Bone (Only spring)" };
    private Vector2 scrollPos;

    private void OnGUI()
    {
        if (vehicle == null)
        {
            Close();
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("Wheel Collider");
        GUILayout.Label("Pivot Position");
        GUILayout.Label("Steer Angle");
        GUILayout.Label("Side");
        GUILayout.Label(" ");
        GUILayout.EndHorizontal();
        GUILayout.Label("-------------------------------------------------------------------------------");
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        int i = 1;
        foreach (VirtualWheel wheel in wheelColliders)
        {

            //wheel collider
            GUILayout.BeginHorizontal();
            GUILayout.Label("Wheel ID: "+i.ToString());
            wheel.pos = (Transform)EditorGUILayout.ObjectField(wheel.pos, typeof(Transform),true);
            wheel.steer_angle = EditorGUILayout.FloatField(wheel.steer_angle);
            wheel.side_int = EditorGUILayout.Popup((int)(wheel.side), side_types);
            if (GUILayout.Button("X"))
            {
                wheelColliders.Remove(wheel);
                GUILayout.EndHorizontal();
                break;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            //sub menu

            if (wheel.sub_wheels_parts.Count>0)
            {
                GUILayout.Label("Controlled sub meshes:");
                for (int j=0; j< wheel.sub_wheels_parts.Count; j++)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("    >");
                    wheel.sub_wheels_parts[j].go = (GameObject)EditorGUILayout.ObjectField(wheel.sub_wheels_parts[j].go, typeof(GameObject), true);
                    wheel.sub_wheels_parts[j].connectedPartType_int = EditorGUILayout.Popup((int)(wheel.sub_wheels_parts[j].connectedPartType), follow_types);
                    if (GUILayout.Button("X"))
                    {
                        wheel.sub_wheels_parts.Remove(wheel.sub_wheels_parts[j]);
                        GUILayout.EndHorizontal();
                        break;
                    }

                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.Space(5);
            //button add sub mesh
            GUILayout.BeginHorizontal();
            GUILayout.Label("     ");
            if (GUILayout.Button("Add controlled sub mesh",GUILayout.MaxWidth(210)))
            {
                wheel.sub_wheels_parts.Add(new WheelSubPart());
            }
            GUILayout.Label("     ");
            GUILayout.EndHorizontal();


            GUILayout.Space(25);
            i++;
        }


        //button add wheel collider
        if (i<=8)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            GUILayout.Label("  ");
            if (GUILayout.Button("Add wheel collider", GUILayout.MaxWidth(210)))
            {
                VirtualWheel vw = new VirtualWheel(null);
                wheelColliders.Add(vw);
            }
            GUILayout.Label("   ");
            GUILayout.EndHorizontal();
        }

        //inputs for tracks
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Left Track (Optional)", GUILayout.MaxWidth(180));
        leftTrack = (Renderer)EditorGUILayout.ObjectField(leftTrack, typeof(Renderer), true);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Right Track (Optional)", GUILayout.MaxWidth(180));
        rightTrack = (Renderer)EditorGUILayout.ObjectField(rightTrack, typeof(Renderer), true);
        GUILayout.EndHorizontal();

        //finalize window
        GUILayout.Space(5);
        if (GUILayout.Button("Finalize", GUILayout.MaxWidth(140)))
        {
            FinalizeWheels();
            Close();
        }

        GUILayout.EndScrollView();
    }

    public static void Init(GameObject[] wheels)
    {
        wheelColliders.Clear();
        vehicle = null;

        int i = 0;
        foreach (GameObject wheel in wheels)
        {
            if (i>=8)
            {
                Debug.LogError("A vehicle can have a maximum amount of 8 wheel colliders! You can fake the other wheels in the wheel collider set up menu.");
                break;
            }

            if (wheel != null && ModTemplates.IsInstance(wheel))
            {
                VirtualWheel vw = new VirtualWheel(wheel.transform);
                vw.sub_wheels_parts.Add(new WheelSubPart(wheel.gameObject));
                wheelColliders.Add(vw);

                i++;
            }

            if (vehicle == null)
            {
                vehicle = wheel.GetComponentInParent<Vehicle>();
            }
        }


        //set side of the wheel automatically
        bool useSteeringWHeels = vehicle == null || !(vehicle is VehicleTank) && !(vehicle is VehiclePlane);
        FixFarWheels(wheelColliders, useSteeringWHeels);
    }
    public static void SetUpWheelsSimple(GameObject[] wheels)
    {
        Init(wheels);

        //finalzie
        FinalizeWheels();
    }



    private static void FinalizeWheels()
    {
        //errors
        if (vehicle == null)
            return;

        if (Selection.objects.Length > 8)
        {
            Debug.LogError("Wheeled vehicles cannot have more than 8 wheels for performance reasons, set them up as tracks instead");
            return;
        }


        //master wheel parent
        GameObject wheelsRoot = new GameObject("WheelsRoot");
        wheelsRoot.transform.SetParent(vehicle.transform);
        wheelsRoot.transform.localPosition = Vector3.zero;
        //wheel meshes parent
        GameObject wmesh = new GameObject("WheelMeshes");
        wmesh.transform.SetParent(wheelsRoot.transform);
        wmesh.transform.localPosition = Vector3.zero;
        //wheel colliders parent
        GameObject wcolls = new GameObject("WheelColliders");
        wcolls.transform.SetParent(wheelsRoot.transform);
        wcolls.transform.localPosition = Vector3.zero;


        //set up wheels arrays
        List<WheelCollider> leftWheels = new List<WheelCollider>();
        List<WheelCollider> rightWheels = new List<WheelCollider>();


        //add wheel basic components
        foreach (VirtualWheel wheelData in wheelColliders)
        {
            //skip if null
            if (!wheelData.pos)
            {
                Debug.LogError("Missing pivot position in one wheel collider!");
                continue;
            }

            //unpack prefab
            ModTemplates.UnpackPrefab(wheelData.pos.gameObject);
            foreach (WheelSubPart wup in wheelData.sub_wheels_parts)
            {
                if (wup.go!=null)
                    ModTemplates.UnpackPrefab(wup.go);
            }

            //add whell collider data
            WheelColliderUpdater wheel = SetUpWheel(wheelData, wmesh, wcolls);
            if (wheel != null)
            {
                if (wheel.side==WheelColliderSide.Left)
                    leftWheels.Add(wheel.GetComponent<WheelCollider>());
                else
                    rightWheels.Add(wheel.GetComponent<WheelCollider>());
            }
        }


        //register wheels in root
        if (vehicle is VehicleWithWheels)
        {
            VehicleWithWheels veh_withWheels = (VehicleWithWheels)vehicle;
            veh_withWheels.leftTrack = leftWheels.ToArray();
            veh_withWheels.rightTrack = rightWheels.ToArray();

            //register tracks
            if (leftTrack || rightTrack)
            {
                TrackWeelsUpdater twu = veh_withWheels.GetComponent<TrackWeelsUpdater>();
                if (twu == null) twu = veh_withWheels.gameObject.AddComponent<TrackWeelsUpdater>();
                twu.leftTrack = leftTrack;
                twu.rightTrack = rightTrack;
            }
        }
        else if (vehicle is Glider)
        {
            Glider veh_glider = (Glider)vehicle;
            if (!veh_glider.GetComponent<WheelsBrakeSetter>())
                veh_glider.gameObject.AddComponent<WheelsBrakeSetter>();
            WheelsBrakeSetter wbs = veh_glider.GetComponent<WheelsBrakeSetter>();
            leftWheels.AddRange(rightWheels);//all wheels
            wbs.wheels = leftWheels.ToArray();
        }
        else if (vehicle is VehiclePlane)
        {
            VehiclePlane veh_plane = (VehiclePlane)vehicle;
            leftWheels.AddRange(rightWheels);//all wheels
            veh_plane.brakingWheels = leftWheels.ToArray();
        }
    }

    private static WheelColliderUpdater SetUpWheel(VirtualWheel selectedWheel, GameObject root, GameObject collRoot)
    {
        //set up collision wheel position
        GameObject collPivot = new GameObject("Collision Pivot " + selectedWheel.pos.gameObject.name);
        collPivot.transform.SetParent(collRoot.transform);
        collPivot.transform.position = selectedWheel.pos.position;


        //Setting up wheel collider
        WheelCollider wc = collPivot.AddComponent<WheelCollider>();
        WheelColliderUpdater wcu = collPivot.AddComponent<WheelColliderUpdater>();
        wcu.steer_angle = selectedWheel.steer_angle;
        wcu.side = selectedWheel.side;
        wc.mass = 20;
        wc.radius = 0.4f;
        wc.wheelDampingRate = 0.25f;
        wc.suspensionDistance = 0.12f;
        wc.forceAppPointDistance = 0;
        JointSpring js = new JointSpring();
        js.spring = 45000;
        js.damper = 4500;
        js.targetPosition = 0.5f;
        wc.suspensionSpring = js;
        WheelFrictionCurve wfc = new WheelFrictionCurve();
        wfc.extremumSlip = 0.4f;
        wfc.extremumValue = 1;
        wfc.asymptoteSlip = 0.5f;
        wfc.asymptoteValue = 0.75f;
        wfc.stiffness = 1;
        wc.forwardFriction = wfc;
        WheelFrictionCurve sf = new WheelFrictionCurve();
        sf.extremumSlip = 0.2f;
        sf.extremumValue = 1;
        sf.asymptoteSlip = 0.5f;
        sf.asymptoteValue = 0.75f;
        sf.stiffness = 1;
        wc.sidewaysFriction = sf;


        //set up mesh wheels and their pivots
        List<Transform> wheels_FollowCompletly = new List<Transform>();
        List<Transform> wheels_OnlyRotation = new List<Transform>();
        foreach (WheelSubPart wup in selectedWheel.sub_wheels_parts)
        {
            if (wup!=null)
            {
                if (wup.connectedPartType!= ConnectedPartType.track_bone)
                {
                    GameObject pivot = new GameObject("Pivot " + wup.go.name);
                    pivot.transform.SetParent(root.transform);
                    pivot.transform.position = wup.go.transform.position;
                    wup.go.transform.SetParent(pivot.transform);
                }

                switch (wup.connectedPartType)
                {
                    case ConnectedPartType.follow_all:
                        wheels_FollowCompletly.Add(wup.go.transform);
                        break;
                    case ConnectedPartType.follow_spring:
                        wheels_OnlyRotation.Add(wup.go.transform);
                        break;
                    case ConnectedPartType.track_bone:
                        wcu.connectedBone = wup.go.transform;
                        break;
                }

            }
        }
        wcu.wheelsTransform = wheels_FollowCompletly.ToArray();
        wcu.wheelsTransform_syncOnlyRot = wheels_OnlyRotation.ToArray();


        //done
        return wcu;
    }

    private static void FixFarWheels(List<VirtualWheel> wheelColliders, bool use_steering_wheels)
    {
        VirtualWheel leftFrontWheel = null, rightFrontWheel = null;
        foreach (VirtualWheel wheel in wheelColliders)
        {
            wheel.side = wheel.pos.localPosition.x >= 0 ?
                WheelColliderSide.Right :
                WheelColliderSide.Left;
            if (wheel.side == WheelColliderSide.Left)
            {
                if (leftFrontWheel == null || leftFrontWheel.pos.localPosition.z < wheel.pos.localPosition.z)
                    leftFrontWheel = wheel;
            }
            else
            {
                if (rightFrontWheel == null || rightFrontWheel.pos.localPosition.z < wheel.pos.localPosition.z)
                    rightFrontWheel = wheel;
            }
        }

        if (use_steering_wheels)
        {
            if (leftFrontWheel != null)
                leftFrontWheel.steer_angle = 40;
            if (rightFrontWheel != null)
                rightFrontWheel.steer_angle = 40;
        }
    }

}


public class VirtualWheel
{
    public Transform pos;
    public float steer_angle;
    public WheelColliderSide side;
    public List<WheelSubPart> sub_wheels_parts = new List<WheelSubPart>();

    public int side_int { set { side = value == 0 ? WheelColliderSide.Left : WheelColliderSide.Right; } }

    public VirtualWheel(Transform pos)
    {
        this.pos = pos;
        this.steer_angle = 0;
    }
}
public class WheelSubPart
{
    public GameObject go=null;
    public ConnectedPartType connectedPartType;

    public WheelSubPart(GameObject connected_go=null)
    {
        this.go = connected_go;
    }

    public int connectedPartType_int { set {
            if (value == 0)
                connectedPartType = ConnectedPartType.follow_all;
            else if (value == 1)
                connectedPartType = ConnectedPartType.follow_spring;
            else if(value == 2)
                connectedPartType = ConnectedPartType.track_bone;
        } }
}
public enum ConnectedPartType
{
    follow_all=0,
    follow_spring=1,
    track_bone=2
}
#endif