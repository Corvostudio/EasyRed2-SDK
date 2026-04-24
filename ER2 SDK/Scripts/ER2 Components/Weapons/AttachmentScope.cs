using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class AttachmentScope : Attachment
{

    public Transform aimPos;
    [Range(20, 65)]
    public float aimingFOV = 30;

}
