using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using System.Collections;
using Unity.VisualScripting;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject reticlePanel;

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

    [SerializeField, Header("In Lobby")] private GameObject inLobbyPanel;
    [SerializeField] private Button exitLobbyButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TMP_InputField inLobbyCodeInput;
    [SerializeField] private Transform playerListParent;
    [SerializeField] private GameObject playerListItemPrefab;
    [SerializeField] private Button startGameButton;

    [SerializeField] private Image popupPanel;
    [SerializeField] private TextMeshProUGUI popupText;

    [SerializeField] private Sprite unmutedIcon;
    [SerializeField] private Sprite mutedIcon;

    private struct PlayerListItem
    {
        public string playerId;
        public GameObject root;
        public Image background;
        public TextMeshProUGUI nameText;
        public Image nameBackground;
        public Button kickButton;
        public GameObject hostIcon;
        public Button muteButton;
    }
    private Dictionary<string, PlayerListItem> playerListItems = new Dictionary<string, PlayerListItem>();

    private void Start()
    {
        playerNameInput.onValueChanged.AddListener(ctx => LobbyManager.Instance.playerName = ctx);
        playerNameInput.onEndEdit.AddListener(EndEditPlayerName);
        nameColorButton.onClick.AddListener(ClickNameColorButton);
        playerColorButton.onClick.AddListener(ClickPlayerColorButton);
        colorPicker.onColorChanged += color => ChangeColorPicker(color);
        colorPicker.gameObject.SetActive(false);

        lobbyNameInput.onEndEdit.AddListener(ctx => EndEditLobbySettings());
        maxPlayersInput.onEndEdit.AddListener(ctx => EndEditLobbySettings());
        isPrivateToggle.onValueChanged.AddListener(IsPrivateChanged);

        exitLobbyButton.onClick.AddListener(ExitLobby);
        startGameButton.onClick.AddListener(() => startGameButton.gameObject.SetActive(false));
        startGameButton.onClick.AddListener(LobbyManager.Instance.StartGame);

        LobbyManager.Instance.OnLobbyChanged += LobbyChanged;
        LobbyManager.Instance.OnKickedFromLobby += OnKick;
        LobbyManager.Instance.OnPlayerJoined += PlayerJoined;
        LobbyManager.Instance.OnPlayerLeft += PlayerLeft;
        LobbyManager.Instance.OnPlayerDataChanged += PlayerDataChanged;
        LobbyManager.Instance.OnJoinLobbyFailed += FailedToJoinLobby;

        playerNameInput.text = LobbyManager.Instance.playerName;
        nameColorButton.GetComponent<Image>().color = LobbyManager.Instance.playerNameColor;
        playerColorButton.GetComponent<Image>().color = LobbyManager.Instance.playerColor;

        if(LobbyManager.Instance.hostLobby != null)
        {
            lobbyNameInput.text = LobbyManager.Instance.hostLobby.Name;
            maxPlayersInput.text = LobbyManager.Instance.hostLobby.MaxPlayers.ToString();
            isPrivateToggle.isOn = LobbyManager.Instance.hostLobby.IsPrivate;
            startGameButton.gameObject.SetActive(true);
        }
        else
        {
            startGameButton.gameObject.SetActive(false);
        }

        lobbyNameText.text = LobbyManager.Instance.joinedLobby.Name;
        inLobbyCodeInput.text = LobbyManager.Instance.joinedLobby.LobbyCode;
        foreach(Player player in LobbyManager.Instance.joinedLobby.Players)
        {
            CreatePlayerListItem(player);
        }
    }

    private void Update()
    {
        if (PlayerControl.Instance != null && PlayerControl.Instance.inputActions.UI.Menu.triggered)
        {
            reticlePanel.SetActive(!PlayerControl.Instance.paused);
            inLobbyPanel.SetActive(PlayerControl.Instance.paused);
            playerSettingsPanel.SetActive(PlayerControl.Instance.paused);
            if(LobbyManager.Instance.hostLobby != null)
            {
                lobbySettingsPanel.SetActive(PlayerControl.Instance.paused);
            }
        }
    }

    private void LobbyChanged(ILobbyChanges changes)
    {
        inLobbyCodeInput.text = LobbyManager.Instance.joinedLobby.LobbyCode;

        if (changes.Name.Changed)
        {
            lobbyNameText.text = changes.Name.Value;
        }

        if (changes.HostId.Changed)
        {
            if (changes.HostId.Value == AuthenticationService.Instance.PlayerId)
            {
                playerListItems[AuthenticationService.Instance.PlayerId].hostIcon.SetActive(true);
                LobbyManager.Instance.hostLobby = LobbyManager.Instance.joinedLobby;
                foreach (PlayerListItem playerListItem in playerListItems.Values)
                {
                    playerListItem.kickButton.gameObject.SetActive(playerListItem.playerId != AuthenticationService.Instance.PlayerId);
                }
                startGameButton.gameObject.SetActive(true);
            }
            else
            {
                playerListItems[AuthenticationService.Instance.PlayerId].hostIcon.SetActive(false);
                LobbyManager.Instance.hostLobby = null;
                foreach (PlayerListItem playerListItem in playerListItems.Values)
                {
                    playerListItem.kickButton.gameObject.SetActive(false);
                }
                startGameButton.gameObject.SetActive(false);
            }
        }
    }

    private void PlayerJoined(List<LobbyPlayerJoined> players)
    {
        foreach(LobbyPlayerJoined player in players)
        {
            if (!playerListItems.ContainsKey(player.Player.Id))
            {
                CreatePlayerListItem(player.Player);
            }
        }
    }

    private void PlayerLeft(List<int> players)
    {
        foreach(int index in players)
        {
            string playerId = LobbyManager.Instance.joinedLobby.Players[index].Id;
            if (playerListItems.ContainsKey(playerId))
            {
                Destroy(playerListItems[playerId].root);
                playerListItems.Remove(playerId);
            }

            // Host leaving resets all network objects and reloads the scene
            if(playerId == LobbyManager.Instance.joinedLobby.HostId)
            {
                ExitLobby();
                StartCoroutine(Popup("Host left the lobby.", new Color(0.4f, 0f, 0f, 0.9f), Color.red, 2f));
            }
        }
    }

    private void CreatePlayerListItem(Player player)
    {
        Dictionary<string, PlayerDataObject> data = player.Data;

        GameObject playerListItemGO = Instantiate(playerListItemPrefab, playerListParent);
        PlayerListItem playerListItem = new PlayerListItem();
        playerListItem.playerId = player.Id;
        playerListItem.root = playerListItemGO;

        Image background = playerListItemGO.GetComponent<Image>();
        playerListItem.background = background;
        string[] colorData = data["color"].Value.Split(" ");
        Color playerColor = new Color(float.Parse(colorData[0]), float.Parse(colorData[1]), float.Parse(colorData[2]), float.Parse(colorData[3]));
        background.color = playerColor;

        Image nameBackground = playerListItemGO.transform.Find("Name").GetComponent<Image>();
        playerListItem.nameBackground = nameBackground;

        TextMeshProUGUI nameText = nameBackground.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        nameText.text = data["name"].Value;
        playerListItem.nameText = nameText;

        string[] nameColorData = data["nameColor"].Value.Split(" ");
        Color nameColor = new Color(float.Parse(nameColorData[0]), float.Parse(nameColorData[1]), float.Parse(nameColorData[2]), float.Parse(nameColorData[3]));
        nameText.color = nameColor;
        nameBackground.color = new Color(1 - nameColor.r, 1 - nameColor.g, 1 - nameColor.b, nameColor.a);

        Transform iconsParent = playerListItemGO.transform.Find("Icons");

        Button muteButton = iconsParent.Find("Mute Button").GetComponent<Button>();
        playerListItem.muteButton = muteButton;
        muteButton.onClick.AddListener(() => ToggleMute(muteButton, player.Id));

        Button kickButton = iconsParent.Find("Kick Button").GetComponent<Button>();
        playerListItem.kickButton = kickButton;
        kickButton.onClick.AddListener(() => LobbyManager.Instance.KickPlayer(player.Id));
        if (LobbyManager.Instance.hostLobby == null || player.Id == AuthenticationService.Instance.PlayerId)
        {
            kickButton.gameObject.SetActive(false);
        }

        GameObject hostIcon = iconsParent.Find("Host Icon").gameObject;
        playerListItem.hostIcon = hostIcon;
        hostIcon.SetActive(LobbyManager.Instance.joinedLobby.HostId == player.Id);

        playerListItems.Add(player.Id, playerListItem);
    }

    private void PlayerDataChanged(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> playerDataChanges)
    {
        foreach(int index in playerDataChanges.Keys)
        {
            string playerId = LobbyManager.Instance.joinedLobby.Players[index].Id;
            foreach(string key in playerDataChanges[index].Keys)
            {
                if (playerDataChanges[index][key].Changed)
                {
                    if (playerListItems.ContainsKey(playerId))
                    {
                        switch (key)
                        {
                            case "name":
                                playerListItems[playerId].nameText.text = playerDataChanges[index][key].Value.Value;                                    
                                break;
                            case "nameColor":
                                string[] nameColorData = playerDataChanges[index][key].Value.Value.Split(" ");
                                Color nameColor = new Color(float.Parse(nameColorData[0]), float.Parse(nameColorData[1]), float.Parse(nameColorData[2]), float.Parse(nameColorData[3]));
                                playerListItems[playerId].nameText.color = nameColor;
                                playerListItems[playerId].nameBackground.color = new Color(1 - nameColor.r, 1 - nameColor.g, 1 - nameColor.b, nameColor.a);
                                break;
                            case "color":
                                string[] colorData = playerDataChanges[index][key].Value.Value.Split(" ");
                                Color playerColor = new Color(float.Parse(colorData[0]), float.Parse(colorData[1]), float.Parse(colorData[2]), float.Parse(colorData[3]));
                                playerListItems[playerId].background.color = playerColor;
                                break;
                        }
                    }
                    else
                    {
                        CreatePlayerListItem(LobbyManager.Instance.joinedLobby.Players[index]);
                    }
                }
            }
        }
    }

    private void ToggleMute(Button button, string playerId)
    {
        Image buttonImage = button.GetComponent<Image>();
        if(button.GetComponent<Image>().sprite == mutedIcon)
        {
            if (playerId == AuthenticationService.Instance.PlayerId)
            {
                ChatManager.Instance.UnmuteSelf();
            }
            else
            {
                ChatManager.Instance.UnmutePlayerLocally(playerId);
            }
            buttonImage.sprite = unmutedIcon;
            buttonImage.color = Color.black;
        }
        else
        {
            if (playerId == AuthenticationService.Instance.PlayerId)
            {
                ChatManager.Instance.MuteSelf();
            }
            else
            {
                ChatManager.Instance.MutePlayerLocally(playerId);
            }
            buttonImage.sprite = mutedIcon;
            buttonImage.color = Color.red;
        }
    }

    private void EndEditPlayerName(string val)
    {
        if (LobbyManager.Instance.joinedLobby != null)
        {
            playerListItems[AuthenticationService.Instance.PlayerId].nameText.text = val;
        }
        LobbyManager.Instance.playerName = val;
        LobbyManager.Instance.UpdatePlayerData();
    }

    private void ClickNameColorButton()
    {
        choosingPlayerColor = false;
        choosingNameColor = !choosingNameColor;
        colorPicker.gameObject.SetActive(choosingNameColor || choosingPlayerColor);
        Color nameColor = nameColorButton.GetComponent<Image>().color;
        colorPicker.color = nameColor;
        if (LobbyManager.Instance.joinedLobby != null)
        {
            playerListItems[AuthenticationService.Instance.PlayerId].nameText.color = nameColor;
            playerListItems[AuthenticationService.Instance.PlayerId].nameBackground.color = new Color(1 - nameColor.r, 1 - nameColor.g, 1 - nameColor.b, nameColor.a);
        }
        LobbyManager.Instance.UpdatePlayerData();
    }

    private void ClickPlayerColorButton()
    {
        choosingNameColor = false;
        choosingPlayerColor = !choosingPlayerColor;
        colorPicker.gameObject.SetActive(choosingNameColor || choosingPlayerColor);
        Color playerColor = playerColorButton.GetComponent<Image>().color;
        colorPicker.color = playerColor;
        if (LobbyManager.Instance.joinedLobby != null)
        {
            playerListItems[AuthenticationService.Instance.PlayerId].background.color = playerColor;
        }
        LobbyManager.Instance.UpdatePlayerData();
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

    private void ExitLobby()
    {
        LobbyManager.Instance.LeaveLobby();
        LobbyManager.Instance.voluntaryLeave = true;
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
        StartCoroutine(Popup("Failed to join: " + message, new Color(0.4f, 0f, 0f, 0.9f), Color.red, 1.5f));
    }

    private void OnDestroy()
    {
        LobbyManager.Instance.OnLobbyChanged -= LobbyChanged;
        LobbyManager.Instance.OnKickedFromLobby -= OnKick;
        LobbyManager.Instance.OnPlayerJoined -= PlayerJoined;
        LobbyManager.Instance.OnPlayerLeft -= PlayerLeft;
        LobbyManager.Instance.OnPlayerDataChanged -= PlayerDataChanged;
    }
}
