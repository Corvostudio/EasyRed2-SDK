#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/TextureGeneratorParameters", order = 1)]
public class TextureGeneratorParameters : ScriptableObject
{
    public float objectSize = 20;
    public float aperture = 7.5f;
    public float exposure = 1;
    public Vector3 shiftCenter = new Vector3(0, 3, 0);
}

#endif