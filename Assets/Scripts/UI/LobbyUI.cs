using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System.Collections;

public class LobbyUI : MonoBehaviour
{
    [SerializeField, Header("Player Settings")] private TMP_InputField playerNameInput;
    [SerializeField] private Button nameColorButton;
    [SerializeField] private Button playerColorButton;
    private bool choosingNameColor;
    private bool choosingPlayerColor;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private GameObject playerSettingsPanel;

    [SerializeField, Header("Lobby Settings")] private TMP_InputField lobbyNameInput;
    [SerializeField] private TMP_InputField maxPlayersInput;
    [SerializeField] private Toggle isPrivateToggle;
    [SerializeField] private GameObject lobbySettingsPanel;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button cancelCreateButton;
    [SerializeField] private Button toggleCreateButton;

    [SerializeField, Header("Joining")] private TMP_InputField lobbyCodeInput;
    [SerializeField] private Button joinLobbyButton;
    [SerializeField] private Button joinRandomLobbyButton;
    [SerializeField] private GameObject lobbyListPanel;
    [SerializeField] private Transform lobbyListParent;
    [SerializeField] private GameObject lobbyListItemPrefab;
    [SerializeField] private Button refreshButton;
    [SerializeField] private float listRefreshRate = 15f;
    private float refreshTimer = 0f;
    [SerializeField] private TMP_Dropdown regionDropdown;
    private string[] regions = new string[]
    {
        null,
        "us-east1",
        "us-central1",
        "us-west1",
        "southamerica-east1",
        "europe-north1",
        "europe-central2",
        "europe-west4",
        "asia-southeast1",
        "asia-northeast1",
        "asia-south1",
        "australia-southeast1"
    };

    [SerializeField] private Image popupPanel;
    [SerializeField] private TextMeshProUGUI popupText;

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;

    private void Start()
    {
        playerNameInput.onValueChanged.AddListener(ctx => LobbyManager.Instance.playerName = ctx);
        playerNameInput.onEndEdit.AddListener(EndEditPlayerName);
        nameColorButton.onClick.AddListener(ClickNameColorButton);
        playerColorButton.onClick.AddListener(ClickPlayerColorButton);
        colorPicker.onColorChanged += color => ChangeColorPicker(color);
        colorPicker.gameObject.SetActive(false);

        lobbyNameInput.onValueChanged.AddListener(ctx => LobbySettingsInputFieldChanged());
        maxPlayersInput.onValueChanged.AddListener(ctx => LobbySettingsInputFieldChanged());
        lobbyNameInput.onEndEdit.AddListener(ctx => EndEditLobbySettings());
        maxPlayersInput.onEndEdit.AddListener(ctx => EndEditLobbySettings());
        isPrivateToggle.onValueChanged.AddListener(IsPrivateChanged);
        createLobbyButton.onClick.AddListener(CreateLobby);
        cancelCreateButton.onClick.AddListener(() => lobbySettingsPanel.SetActive(false));
        toggleCreateButton.onClick.AddListener(ToggleCreateLobby);
        refreshButton.onClick.AddListener(RefreshLobbyList);
        regionDropdown.onValueChanged.AddListener(ctx => LobbyManager.Instance.SetRegion(regions[ctx]));

        lobbyCodeInput.onValueChanged.AddListener(ctx => JoinInputFieldChanged());
        joinLobbyButton.onClick.AddListener(JoinLobby);
        joinRandomLobbyButton.onClick.AddListener(JoinRandomLobby);

        LobbyManager.Instance.OnJoinLobby += ctx => OnJoinLobby();
        LobbyManager.Instance.OnKickedFromLobby += OnKick;
        LobbyManager.Instance.OnJoinLobbyFailed += FailedToJoinLobby;

        if (LobbyManager.Instance.playerNameColor != Color.clear)
        {
            playerNameInput.text = LobbyManager.Instance.playerName;
            nameColorButton.GetComponent<Image>().color = LobbyManager.Instance.playerNameColor;
            playerColorButton.GetComponent<Image>().color = LobbyManager.Instance.playerColor;
        }
        else
        {
            LobbyManager.Instance.playerNameColor = nameColorButton.GetComponent<Image>().color;
            LobbyManager.Instance.playerColor = playerColorButton.GetComponent<Image>().color;
        }
    }

    private void Update()
    {
        if (AuthenticationService.Instance.IsSignedIn && LobbyManager.Instance.joinedLobby == null)
        {
            if (refreshTimer > listRefreshRate)
            {
                refreshTimer = 0f;
                RefreshLobbyList();
            }
            refreshTimer += Time.deltaTime;
        }
    }

    private void EndEditPlayerName(string val)
    {
        LobbyManager.Instance.playerName = val;
    }

    private void ClickNameColorButton()
    {
        choosingPlayerColor = false;
        choosingNameColor = !choosingNameColor;
        colorPicker.gameObject.SetActive(choosingNameColor || choosingPlayerColor);
        Color nameColor = nameColorButton.GetComponent<Image>().color;
        colorPicker.color = nameColor;
    }

    private void ClickPlayerColorButton()
    {
        choosingNameColor = false;
        choosingPlayerColor = !choosingPlayerColor;
        colorPicker.gameObject.SetActive(choosingNameColor || choosingPlayerColor);
        Color playerColor = playerColorButton.GetComponent<Image>().color;
        colorPicker.color = playerColor;
    }

    private void ChangeColorPicker(Color color)
    {
        if (choosingNameColor)
        {
            playerNameInput.textComponent.color = color;
            nameColorButton.GetComponent<Image>().color = color;
            LobbyManager.Instance.playerNameColor = color;
        }
        else if (choosingPlayerColor)
        {
            playerColorButton.GetComponent<Image>().color = color;
            LobbyManager.Instance.playerColor = color;
        }
    }

    private void LobbySettingsInputFieldChanged()
    {
        createLobbyButton.interactable = !string.IsNullOrEmpty(lobbyNameInput.text) && !string.IsNullOrEmpty(maxPlayersInput.text) && int.Parse(maxPlayersInput.text) > 0;
    }

    private void EndEditLobbySettings()
    {
        if (LobbyManager.Instance.hostLobby != null)
        {
            if (!string.IsNullOrEmpty(lobbyNameInput.text) && !string.IsNullOrEmpty(maxPlayersInput.text))
            {
                int maxPlayers = int.Parse(maxPlayersInput.text);
                if (maxPlayers > 0)
                {
                    LobbyManager.Instance.UpdateLobbySettings(lobbyNameInput.text, maxPlayers);
                }
            }
        }
    }

    private void IsPrivateChanged(bool value)
    {
        LobbyManager.Instance.UpdateLobbySettings(isPrivate: value);
    }

    private void CreateLobby()
    {
        if (!string.IsNullOrEmpty(lobbyNameInput.text) && !string.IsNullOrEmpty(maxPlayersInput.text))
        {
            int maxPlayers = int.Parse(maxPlayersInput.text);
            if (maxPlayers > 0)
            {
                createLobbyButton.interactable = false;
                toggleCreateButton.interactable = false;
                LobbyManager.Instance.CreateLobby(lobbyNameInput.text, maxPlayers, isPrivateToggle.isOn);
                refreshTimer = 0f;
            }
        }
    }

    private void ToggleCreateLobby()
    {
        lobbySettingsPanel.SetActive(!lobbySettingsPanel.activeSelf);
    }

    private void JoinInputFieldChanged()
    {
        createLobbyButton.interactable = !string.IsNullOrEmpty(lobbyCodeInput.text);
    }

    private void JoinLobby()
    {
        if (!string.IsNullOrEmpty(lobbyCodeInput.text))
        {
            loadingPanel.SetActive(true);
            loadingText.text = "Joining lobby...";
            LobbyManager.Instance.JoinLobbyByCode(lobbyCodeInput.text);
        }
    }

    private void OnJoinLobby()
    {
        loadingText.text = "Connecting...";
    }

    private void JoinRandomLobby()
    {
        loadingPanel.SetActive(true);
        loadingText.text = "Joining lobby...";
        LobbyManager.Instance.QuickJoinLobby();
    }

    private void RefreshLobbyList()
    {
        LobbyManager.Instance.ListLobbies();
        for (int i = lobbyListParent.childCount - 1; i >= 0; i--)
        {
            Destroy(lobbyListParent.GetChild(i).gameObject);
        }

        foreach (Lobby lobby in LobbyManager.Instance.lobbyList)
        {
            Transform lobbyListItem = Instantiate(lobbyListItemPrefab, lobbyListParent).transform;
            lobbyListItem.Find("Name").GetComponent<TextMeshProUGUI>().text = lobby.Name;
            lobbyListItem.Find("Players").GetComponent<TextMeshProUGUI>().text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
            lobbyListItem.Find("Region").GetComponent<TextMeshProUGUI>().text = lobby.Data[LobbyManager.REGION_KEY].Value;
            lobbyListItem.GetComponent<Button>().onClick.AddListener(() => { LobbyManager.Instance.JoinLobbyById(lobby.Id); });
        }
    }

    private void OnKick()
    {
        if (LobbyManager.Instance.voluntaryLeave)
        {
            LobbyManager.Instance.voluntaryLeave = false;
        }
        else
        {
            StartCoroutine(Popup("You were kicked from the lobby.", new Color(0.4f, 0f, 0f, 0.9f), Color.red, 2f));
        }
    }

    private IEnumerator Popup(string text, Color backgroundColor, Color textColor, float duration)
    {
        popupPanel.gameObject.SetActive(true);
        popupPanel.color = backgroundColor;
        popupText.text = text;
        popupText.color = textColor;
        yield return new WaitForSeconds(duration);
        popupPanel.gameObject.SetActive(false);
    }

    private void FailedToJoinLobby(string message)
    {
        loadingPanel.SetActive(false);
        StartCoroutine(Popup("Failed to join: " + message, new Color(0.4f, 0f, 0f, 0.9f), Color.red, 1.5f));
    }

    private void OnDestroy()
    {
        LobbyManager.Instance.OnJoinLobby -= ctx => OnJoinLobby();
        LobbyManager.Instance.OnKickedFromLobby -= OnKick;
    }
}
