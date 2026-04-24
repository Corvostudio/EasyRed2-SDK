using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if PHOTON_UNITY_NETWORKING
using Photon.Pun;

public partial class Syncher : PhotonView
{

}

#else

public partial class Syncher : MonoBehaviour
{

}

#endif