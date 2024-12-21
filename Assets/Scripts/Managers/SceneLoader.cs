using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using Unity.Netcode;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    public bool isLoading { get; private set; }

    public event Action<string> OnStartSceneLoad;
    public event Action<string> OnSceneLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
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
        loadingScreen.SetActive(true);
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        asyncLoad.allowSceneActivation = false;
        // Async load progress goes from 0 to 0.9
        float progress = asyncLoad.progress / 0.9f;
        while (progress < 0.9f)
        {
            progress = asyncLoad.progress / 0.9f;
            loadingBar.value = progress;
            loadingText.text = Mathf.RoundToInt(progress * 100f) + "%";
            yield return new WaitForEndOfFrame();
        }
        asyncLoad.allowSceneActivation = true;
        loadingBar.value = 1;
        loadingText.text = "100%";
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == sceneName);
        loadingScreen.SetActive(false);
        isLoading = false;
        OnSceneLoaded?.Invoke(sceneName);
    }
}
