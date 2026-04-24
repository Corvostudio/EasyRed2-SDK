using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UploadStarter : MonoBehaviour
{
    void Start()
    {
        if (SceneManager.GetActiveScene().name!="UploadScene")
        {
            //Load Scene UploadScene
            SceneManager.LoadScene("UploadScene", LoadSceneMode.Single);
        }
    }
}