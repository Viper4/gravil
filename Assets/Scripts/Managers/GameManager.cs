using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject networkManager;

    public Dictionary<ulong, NetworkObject> trackedNetworkObjects = new Dictionary<ulong, NetworkObject>();
    public Dictionary<string, PlayerControl> players = new Dictionary<string, PlayerControl>();

    public List<GravityLock> gravityLocks = new List<GravityLock>();

    public InputActions inputActions;

    private void OnEnable()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Instantiate(networkManager);
            SceneManager.sceneLoaded += (scene, mode) => OnSceneLoad();

            inputActions = new InputActions();
            inputActions.Enable();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        inputActions?.Disable();
    }

    private void Update()
    {
        if (inputActions.UI.Fullscreen.triggered)
        {
            Screen.fullScreen = !Screen.fullScreen;
            if (Screen.fullScreen)
            {
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            }
            else
            {
                Screen.fullScreenMode = FullScreenMode.MaximizedWindow;
            }
        }
    }

    public void OnSceneLoad()
    {
        gravityLocks.Clear();
        gravityLocks.AddRange(FindObjectsByType<GravityLock>(FindObjectsSortMode.None));
    }
}
