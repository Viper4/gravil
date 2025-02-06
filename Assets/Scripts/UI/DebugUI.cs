using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    private struct DebugMessage
    {
        public string message;
        public string stackTrace;
        public LogType type;
    }

    private Dictionary<string, DebugMessage> log = new Dictionary<string, DebugMessage>();
    [SerializeField] private GameObject logDisplay;
    [SerializeField] private TextMeshProUGUI logText;

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.inputActions.UI.Debug.triggered)
        {
            logDisplay.SetActive(!logDisplay.activeSelf);
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string displayText = "";

        string[] splitString = logString.Split(':');
        if(splitString.Length > 0)
        {
            string debugKey = splitString[0];
            if (log.ContainsKey(debugKey))
            {
                log[debugKey] = new DebugMessage { message = logString, stackTrace = stackTrace, type = type };
            }
            else
            {
                log.Add(debugKey, new DebugMessage { message = logString, stackTrace = stackTrace, type = type });
            }
        }
        else
        {
            switch (type)
            {
                case LogType.Log:
                    displayText += "<color=white>" + logString + "</color>\n";
                    break;
                case LogType.Warning:
                    displayText += "<color=yellow>" + logString + "</color>\n";
                    break;
                case LogType.Error:
                    displayText += "<color=red>" + logString + "</color>\n";
                    break;
                case LogType.Exception:
                    displayText += "<color=red>" + logString + "</color>\n";
                    break;
                case LogType.Assert:
                    displayText += "<color=red>" + logString + "</color>\n";
                    break;
            }
        }

        foreach (DebugMessage debugMessage in log.Values)
        {
            switch (debugMessage.type)
            {
                case LogType.Log:
                    displayText += "<color=white>" + debugMessage.message + "</color>\n";
                    break;
                case LogType.Warning:
                    displayText += "<color=yellow>" + debugMessage.message + "</color>\n";
                    break;
                case LogType.Error:
                    displayText += "<color=red>" + debugMessage.message + "</color>\n";
                    break;
                case LogType.Exception:
                    displayText += "<color=red>" + debugMessage.message + "</color>\n";
                    break;
                case LogType.Assert:
                    displayText += "<color=red>" + debugMessage.message + "</color>\n";
                    break;
            }
        }

        logText.text = displayText;
    }
}
