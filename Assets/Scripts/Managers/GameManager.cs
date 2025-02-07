using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject networkManager;

    public Dictionary<ulong, NetworkObject> trackedNetworkObjects = new Dictionary<ulong, NetworkObject>();
    public Dictionary<string, PlayerControl> players = new Dictionary<string, PlayerControl>();

    public List<GravityLock> gravityLocks = new List<GravityLock>();

    public InputActions inputActions;

    [SerializeField] private GameObject screenshotUI;
    [SerializeField] private TextMeshProUGUI screenshotText;

    private void OnEnable()
    {
        if (Instance == null)
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
        if (inputActions != null)
        {
            inputActions.Disable();
        }
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

        if (inputActions.Player.Screenshot.triggered)
        {
            DateTime dateTime = DateTime.Now;
            if (!Directory.Exists($"\\Screenshots\\"))
            {
                Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Screenshots\\");
            }
            string path = Directory.GetCurrentDirectory().Replace("\\", "/") + $"/Screenshots/{dateTime.Year}-{dateTime.Month}-{dateTime.Day}_{dateTime.Hour}.{dateTime.Minute}.{dateTime.Second}.png";
            Debug.Log("Screenshot saved to: " + path);
            ScreenCapture.CaptureScreenshot(path);
            StartCoroutine(ScreenshotPopup(path));
        }
    }

    public void OnSceneLoad()
    {
        gravityLocks.Clear();
        gravityLocks.AddRange(FindObjectsByType<GravityLock>(FindObjectsSortMode.None));
    }

    private IEnumerator ScreenshotPopup(string path)
    {
        yield return new WaitForEndOfFrame(); // Wait for screenshot
        screenshotUI.SetActive(true);
        screenshotText.text = "Saved to " + path;
        yield return new WaitForSeconds(2.5f);
        screenshotUI.SetActive(false);
    }
}
