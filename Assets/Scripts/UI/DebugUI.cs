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

    [SerializeField] private GameObject logDisplay;
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private TMP_InputField inputField;

    private bool inputPassword = false;
    private bool cheatsEnabled = false;
    private LinkedList<string> inputHistory = new LinkedList<string>();
    private LinkedListNode<string> currentInput = null;

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        inputField.onSubmit.AddListener(OnInputSubmit);
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        if (GameManager.Instance != null)
        {
            if (cheatsEnabled && (PlayerControl.Instance == null || !PlayerControl.Instance.IsPaused) && GameManager.Instance.inputActions.Player.LoadNextLevel.triggered)
            {
                LobbyManager.Instance.LoadNextLevel();
            }

            if (logDisplay.activeSelf && inputHistory.Count > 0)
            {
                if (GameManager.Instance.inputActions.Player.UpArrow.triggered)
                {
                    if (currentInput == null)
                    {
                        inputField.text = inputHistory.First.Value;
                        currentInput = inputHistory.First;
                    }
                    else
                    {
                        currentInput = currentInput.Next;
                        if (currentInput == null)
                            currentInput = inputHistory.First;
                        inputField.text = currentInput.Value;
                    }
                }

                if (GameManager.Instance.inputActions.Player.DownArrow.triggered)
                {
                    if (currentInput == null)
                    {
                        inputField.text = inputHistory.Last.Value;
                        currentInput = inputHistory.Last;
                    }
                    else
                    {
                        currentInput = currentInput.Previous;
                        if (currentInput == null)
                            currentInput = inputHistory.Last;
                        inputField.text = currentInput.Value;
                    }
                }
            }

            if (GameManager.Instance.inputActions.UI.Debug.triggered)
            {
                logDisplay.SetActive(!logDisplay.activeSelf);
                if (PlayerControl.Instance != null)
                {
                    if (logDisplay.activeSelf)
                    {
                        PlayerControl.Instance.SetPaused(true);
                    }
                    else
                    {
                        PlayerControl.Instance.SetPaused(false);
                    }
                }

            }
        }
    }

    private void OnInputSubmit(string input)
    {
        if (input != "")
        {
            inputHistory.AddFirst(input);
            if (inputHistory.Count > 16)
            {
                inputHistory.RemoveLast();
            }
            inputField.text = "";
            string[] command = input.Split(' ');
            switch (command[0])
            {
                case "clear":
                    logText.text = "";
                    break;
                case "help":
                    logText.text += "<u>Valid Commands</u>\nclear - Clears the log\nhelp - Displays this message\nlevel n - Loads level n\nnext - Loads the next level\n";
                    break;
                case "level":
                    if (cheatsEnabled)
                    {
                        if (command.Length == 2)
                        {
                            if (int.TryParse(command[1], out int level))
                            {
                                LobbyManager.Instance.LoadLevel(level);
                            }
                            else
                            {
                                Debug.LogWarning($"Expected a number for command 'level' but received '{command[1]}'");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Expected 1 argument for command 'level' but received {command.Length - 1}");
                        }
                    }
                    else
                    {
                        Debug.Log("Cheats are disabled");
                    }
                    break;
                case "next":
                    if (cheatsEnabled)
                    {
                        LobbyManager.Instance.LoadNextLevel();
                    }
                    else
                    {
                        Debug.Log("Cheats are disabled");
                    }
                    break;
                case "cheats":
                    if (command.Length == 2)
                    {
                        if (inputPassword)
                        {
                            cheatsEnabled = !cheatsEnabled;
                            if (cheatsEnabled)
                            {
                                Debug.Log("Enabled cheats");
                            }
                            else
                            {
                                Debug.Log("Disabeld cheats");
                            }
                        }
                        else
                        {
                            if (command[1] == "supersecretpwd")
                            {
                                cheatsEnabled = true;
                                inputPassword = true;
                                Debug.Log("Enabled cheats");
                            }
                        }
                    }
                    break;
                default:
                    Debug.LogWarning($"Unknown command '{command[0]}'. Type 'help' for a list of valid commands");
                    break;
            }
        }
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        switch (type)
        {
            case LogType.Log:
                logText.text += "<color=white>" + logString + "</color>\n";
                break;
            case LogType.Warning:
                logText.text += "<color=yellow>" + logString + "</color>\n";
                break;
            case LogType.Error:
                logText.text += "<color=red>" + logString + " - Stack Trace: " + stackTrace + "</color>\n";
                break;
            case LogType.Exception:
                logText.text += "<color=red>" + logString + " - Stack Trace: " + stackTrace + "</color>\n";
                break;
            case LogType.Assert:
                logText.text += "<color=red>" + logString + " - Stack Trace: " + stackTrace + "</color>\n";
                break;
        }
    }
}
