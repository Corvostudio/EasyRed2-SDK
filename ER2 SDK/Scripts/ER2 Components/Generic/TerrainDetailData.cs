
using UnityEngine;
public partial class TerrainDetailData : MonoBehaviour
{
    [Tooltip("How much the detail follows the slope of the ground: 0 = upright, 1 = fully aligned.")]
    [Range(0,1)]
    public float alignToGround = 0;

    //public TerrainDetailType type;
    [Tooltip("Random width scale of each instance: X = minimum, Y = maximum.")]
    public Vector2 minMaxWidth = new Vector2(0.7f, 1.6f);
    [Tooltip("Random height scale of each instance: X = minimum, Y = maximum.")]
    public Vector2 minMaxHeight = new Vector2(0.7f, 1f);
}
