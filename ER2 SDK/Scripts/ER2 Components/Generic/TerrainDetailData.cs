
using UnityEngine;
public partial class TerrainDetailData : MonoBehaviour
{
    [Range(0,1)]
    public float alignToGround = 0;

    //public TerrainDetailType type;
    public Vector2 minMaxWidth = new Vector2(0.7f, 1.6f);
    public Vector2 minMaxHeight = new Vector2(0.7f, 1f);
}
