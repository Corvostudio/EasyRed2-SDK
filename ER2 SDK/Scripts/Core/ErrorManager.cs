using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ErrorManager : MonoBehaviour
{
    public GameObject errorPanel;
    public Text error_text;
    //string error;

    private void Awake()
    {
        errorPanel.SetActive(false);
        Application.logMessageReceived += HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error && errorPanel)
        {
            if (!errorPanel.activeSelf)
                errorPanel.SetActive(true);
            error_text.text += (">"+logString + "\n");

        }
    }

    public void CancelUpload()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void Dismiss()
    {
        //PopUp.gameObject.SetActive(false);
    }

}
