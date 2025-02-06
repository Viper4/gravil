using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using Unity.VisualScripting;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] GameObject canvas;

    [Header("Loading with no progress")] private string loadingText = "Loading...";
    [SerializeField] private TextMeshProUGUI loadingTMP;
    [SerializeField] private float characterSpeed = 0.25f;
    private int characterIndex = 0;
    private int animatedCharacters = 3;
    private float timer = 0;
    [SerializeField, Header("Loading with progress")] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingBarTMP;
    public bool isLoading { get; private set; }

    public event Action<string> OnStartSceneLoad;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += (scene, mode) => HideLoadingScreen();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (loadingTMP.gameObject.activeSelf)
        {
            if (timer > characterSpeed)
            {
                if (characterIndex >= animatedCharacters)
                {
                    loadingTMP.text = loadingText[..^animatedCharacters];
                    characterIndex = 0;
                }
                else
                {
                    loadingTMP.text = loadingText[..^(animatedCharacters - (characterIndex + 1))];
                    characterIndex++;
                }
                timer = 0;
            }
            timer += Time.deltaTime;
        }
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadRoutine(sceneName));
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        OnStartSceneLoad?.Invoke(sceneName);
        isLoading = true;
        canvas.SetActive(true);
        loadingBar.gameObject.SetActive(true);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        asyncLoad.allowSceneActivation = false;
        // Async load progress goes from 0 to 0.9
        float progress = asyncLoad.progress / 0.9f;
        while (progress < 0.9f)
        {
            progress = asyncLoad.progress / 0.9f;
            loadingBar.value = progress;
            loadingBarTMP.text = Mathf.RoundToInt(progress * 100f) + "%";
            yield return new WaitForEndOfFrame();
        }
        asyncLoad.allowSceneActivation = true;
        loadingBar.value = 1;
        loadingBarTMP.text = "100%";
        Scene currentScene = SceneManager.GetActiveScene();
        while (currentScene.name != sceneName)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            currentScene = SceneManager.GetActiveScene();
        }
        canvas.SetActive(false);
        loadingBar.gameObject.SetActive(false);
        isLoading = false;
    }

    public void ShowLoadingScreen(string text, int animatedCharacters = 3)
    {
        canvas.SetActive(true);
        loadingTMP.gameObject.SetActive(true);
        timer = 0;
        characterIndex = 0;
        loadingText = text;
        loadingTMP.text = loadingText[..^animatedCharacters];
        this.animatedCharacters = animatedCharacters;
    }

    public void HideLoadingScreen()
    {
        canvas.SetActive(false);
        loadingTMP.gameObject.SetActive(false);
    }
}
