using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AutoTransportDriverUniform : MonoBehaviour
{
    public SkinnedMeshRenderer uniform, uniform_lod;
    public SkinnedMeshRenderer gear, gear_lod;//optional, for the faction's auto transport gear/vest
    public Transform helmet_pos;
    public GameObject[] setEnabledOnSetClothing = new GameObject[0];
}
