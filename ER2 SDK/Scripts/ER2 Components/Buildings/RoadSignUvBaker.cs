using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed partial class RoadSignUvBaker : MonoBehaviour
{
    [Serializable]
    public sealed class SignTarget
    {
        [Tooltip("Renderer della faccia del cartello (quello col materiale dell'atlante).")]
        public Renderer targetRenderer;

        [Tooltip("Cella su cui la UV è autorata in Blender. È il default del prefab, mai mutato a runtime.")]
        public int atlasSlotIndex = 0;
    }

    public RoadSignAtlasDefinition atlasDefinition;

    [Tooltip("Applica all'OnEnable: gestisce il default e il reset delle istanze riusate dal pool.")]
    public bool applyOnEnable = true;

    [Tooltip("L'inspector applica subito quando cambi un valore.")]
    public bool autoApplyInEditor = true;

    public List<SignTarget> targets = new List<SignTarget>();
}