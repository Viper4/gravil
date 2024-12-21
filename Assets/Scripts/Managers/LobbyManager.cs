using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System;
using Unity.Netcode;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using System.Collections;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    // Lobby Data
    private const string IN_GAME_KEY = "inGame";
    private const string RELAY_JOIN_CODE_KEY = "relayJoinCode";
    public const string REGION_KEY = "region"; // S1 index option

    // Player Data
    private const string PLAYER_NAME_KEY = "name";
    private const string PLAYER_NAME_COLOR_KEY = "nameColor";
    private const string PLAYER_COLOR_KEY = "color";

    public static LobbyManager Instance { get; private set; }

    public Lobby hostLobby;
    public Lobby joinedLobby;
    private string region = null;

    private float heartbeatTimer = 0f;
    private readonly float hearbeatTimerMax = 20f;

    private float lobbyUpdateTimer = 0f;
    private readonly float lobbyUpdateTimerMax = 2f; // Limit is 1 call per 1 second

    [HideInInspector] public string playerName;
    [HideInInspector] public Color playerNameColor;
    [HideInInspector] public Color playerColor;

    public List<Lobby> lobbyList = new List<Lobby>();

    public event Action<string> OnJoinLobby;
    public event Action<ILobbyChanges> OnLobbyChanged;
    public event Action OnKickedFromLobby;
    public event Action<List<LobbyPlayerJoined>> OnPlayerJoined;
    public event Action<List<int>> OnPlayerLeft;
    public event Action<Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>>> OnPlayerDataChanged;
    public event Action<string> OnJoinLobbyFailed;

    private bool leavingLobby;
    public bool voluntaryLeave;

    public bool inGame;
    private string levelName;
    private int levelIndex = -1;

    private async void Start()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneLoader.Instance.OnSceneLoaded += OnSceneLoaded;

        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += OnSignIn;
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void Update()
    {
        HostLobbyHeartbeat();
        UpdateJoinedLobby();
    }

    public void SetRegion(string region)
    {
        this.region = region;
    }

    private async void HostLobbyHeartbeat()
    {
        if (!leavingLobby && hostLobby != null)
        {
            heartbeatTimer += Time.deltaTime;
            if (heartbeatTimer > hearbeatTimerMax)
            {
                heartbeatTimer = 0f;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    private async void UpdateJoinedLobby()
    {
        if (!leavingLobby && joinedLobby != null)
        {
            lobbyUpdateTimer += Time.deltaTime;
            if (lobbyUpdateTimer > lobbyUpdateTimerMax)
            {
                lobbyUpdateTimer = 0f;
                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
            }
        }
    }

    private void OnSignIn()
    {
        Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
    }

    private async void AddLobbyListeners(Lobby lobby)
    {
        LobbyEventCallbacks callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += (ctx) => OnLobbyChanged?.Invoke(ctx);
        callbacks.KickedFromLobby += () => OnKick();
        callbacks.PlayerJoined += (ctx) => OnPlayerJoined?.Invoke(ctx);
        callbacks.PlayerLeft += (ctx) => OnPlayerLeft?.Invoke(ctx);
        callbacks.PlayerDataChanged += (ctx) => OnPlayerDataChanged?.Invoke(ctx);
        callbacks.DataChanged += LobbyDataChanged;

        await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);
    }

    private async Task<Allocation> AllocateRelay(int maxPlayers)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1, region);
            return allocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning("Exception while creating allocation: " + e);
            return default;
        }
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        try
        {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            return relayJoinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning("Exception while getting join code: " + e);
            return default;
        }
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return joinAllocation;
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning("Exception while joining relay: " + e);
            return default;
        }
    }

    public async void CreateLobby(string name, int maxPlayers, bool isPrivate)
    {
        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                IsPrivate = isPrivate,
                Player = GetPlayer()
            };
            hostLobby = await LobbyService.Instance.CreateLobbyAsync(name, maxPlayers, options);
            joinedLobby = hostLobby;

            Allocation allocation = await AllocateRelay(maxPlayers);
            string relayJoinCode = await GetRelayJoinCode(allocation);

            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions()
            {
                Data = new Dictionary<string, DataObject>()
                {
                    { RELAY_JOIN_CODE_KEY, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) },
                    { REGION_KEY, new DataObject(DataObject.VisibilityOptions.Public, allocation.Region, DataObject.IndexOptions.S1) },
                }
            });

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4, 
                (ushort)allocation.RelayServer.Port, 
                allocation.AllocationIdBytes, 
                allocation.Key, 
                allocation.ConnectionData);

            OnJoinLobby?.Invoke(joinedLobby.Id);

            AddLobbyListeners(hostLobby);

            SceneLoader.Instance.LoadScene("Lobby");
            levelName = "Lobby";
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("Exception while creating lobby: " + e);
        }
    }

    public async void ListLobbies()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions()
            {
                Count = 50,
                Filters = new List<QueryFilter>()
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                },
                Order = new List<QueryOrder>()
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };
            if(region != null)
            {
                options.Filters.Add(new QueryFilter(QueryFilter.FieldOptions.S1, region, QueryFilter.OpOptions.EQ));
            }

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
            lobbyList = response.Results;
        }
        catch(LobbyServiceException e)
        {
            Debug.LogWarning("Exception while listing lobbies: " + e);
        }
    }

    private async void JoinLobbyRelay(Lobby lobby)
    {
        JoinAllocation joinAllocation = await JoinRelay(lobby.Data[RELAY_JOIN_CODE_KEY].Value);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData);

        AddLobbyListeners(joinedLobby);

        NetworkManager.Singleton.StartClient();
    }

    public async void JoinLobbyByCode(string code)
    {
        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions()
            {
                Player = GetPlayer()
            };
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code, options);
            OnJoinLobby?.Invoke(joinedLobby.Id);
            JoinLobbyRelay(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("Exception while joining lobby: " + e);
            OnJoinLobbyFailed?.Invoke(e.Reason.ToString());
        }
    }

    public async void JoinLobbyById(string Id)
    {
        try
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions()
            {
                Player = GetPlayer()
            };
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(Id, options);
            OnJoinLobby?.Invoke(joinedLobby.Id);

            JoinLobbyRelay(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("Exception while joining lobby: " + e);
            OnJoinLobbyFailed?.Invoke(e.Reason.ToString());
        }
    }

    public async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions()
            {
                Player = GetPlayer()
            };
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            OnJoinLobby?.Invoke(joinedLobby.Id);

            JoinLobbyRelay(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("Exception while quick joining lobby: " + e);
            OnJoinLobbyFailed?.Invoke(e.Reason.ToString());
        }
    }

    private Player GetPlayer()
    {
        string nameColorStr = playerNameColor.r + " " + playerNameColor.g + " " + playerNameColor.b + " " + playerNameColor.a;
        string playerColorStr = playerColor.r + " " + playerColor.g + " " + playerColor.b + " " + playerColor.a;
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>()
            {
                { PLAYER_NAME_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                { PLAYER_NAME_COLOR_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, nameColorStr) },
                { PLAYER_COLOR_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerColorStr) }
            }
        };
    }

    private async void UpdateGameData(bool? inGame = null)
    {
        try
        {
            if (hostLobby != null)
            {
                Dictionary<string, DataObject> data = new Dictionary<string, DataObject>();
                if (inGame != null)
                {
                    this.inGame = (bool)inGame;
                    data.Add(IN_GAME_KEY, new DataObject(DataObject.VisibilityOptions.Public, this.inGame.ToString()));
                }

                if (data.Count > 0)
                {
                    hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
                    {
                        Data = data
                    });
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("Exception while updating lobby in game data: " + e);
        }
    }

    public async void UpdateLobbySettings(string name = null, int maxPlayers = 0, bool? isPrivate = null)
    {
        try
        {
            UpdateLobbyOptions lobbyOptions = new UpdateLobbyOptions();
            if(name != null)
                lobbyOptions.Name = name;

            if(maxPlayers > 0)
                lobbyOptions.MaxPlayers = maxPlayers;

            if (isPrivate != null)
                lobbyOptions.IsPrivate = (bool)isPrivate;

            if (hostLobby != null)
            {
                hostLobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, lobbyOptions);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("Exception while updating lobby settings: " + e);
        }
    }

    private void LobbyDataChanged(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> changes)
    {
        foreach (string key in changes.Keys)
        {
            if (changes[key].Changed)
            {
                switch (key)
                {
                    case IN_GAME_KEY:
                        inGame = bool.Parse(changes[key].Value.Value);
                        break;
                }
            }
        }
        /*string newScene = levelName + " " + levelIndex;
        if (oldScene != newScene)
        {
            SceneLoader.Instance.LoadScene(newScene);
        }*/
    }

    /// <summary>
    /// Updates this player's data.
    /// </summary>
    public async void UpdatePlayerData()
    {
        try
        {
            if (joinedLobby != null)
            {
                string nameColorStr = playerNameColor.r + " " + playerNameColor.g + " " + playerNameColor.b + " " + playerNameColor.a;
                string playerColorStr = playerColor.r + " " + playerColor.g + " " + playerColor.b + " " + playerColor.a;
                await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>()
                    {
                        { PLAYER_NAME_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                        { PLAYER_NAME_COLOR_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, nameColorStr) },
                        { PLAYER_COLOR_KEY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerColorStr) }
                    }
                });
                if (PlayerControl.Instance != null)
                {
                    PlayerControl.Instance.UpdatePlayerServerRpc(playerName, playerNameColor, playerColor, AuthenticationService.Instance.PlayerId);
                    //PlayerControl.Instance.UpdatePlayerClientRpc(playerName, playerNameColor, playerColor);
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("Exception while updating player name: " + e);
        }
    }

    public async void LeaveLobby()
    {
        try
        {
            if (joinedLobby != null)
            {
                leavingLobby = true;
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;
                hostLobby = null;
                leavingLobby = false;
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("Exception while leaving lobby: " + e);
            leavingLobby = false;
        }
    }

    public async void KickPlayer(string playerId)
    {
        try
        {
            if (hostLobby != null)
            {
                await LobbyService.Instance.RemovePlayerAsync(hostLobby.Id, playerId);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning("Exception while updating player name: " + e);
        }
    }

    public void StartGame()
    {
        if (hostLobby != null)
        {
            Debug.Log("Start game host");
            UpdateGameData(true);
            levelName = "Level";
            levelIndex = 0;
            NetworkManager.Singleton.SceneManager.LoadScene(levelName + " " + levelIndex, LoadSceneMode.Single);
            //SceneLoader.Instance.LoadScene(levelName + " " + levelIndex);
        }
    }

    public void LoadNextLevel()
    {
        if (hostLobby != null)
        {
            Debug.Log("Load next level");
            levelIndex++;
            NetworkManager.Singleton.SceneManager.LoadScene(levelName + " " + levelIndex, LoadSceneMode.Single);
            //SceneLoader.Instance.LoadScene(levelName + " " + levelIndex);
        }
    }

    private void OnSceneLoaded(string sceneName)
    {
        if (!NetworkManager.Singleton.IsClient)
        {
            if (hostLobby != null)
            {
                NetworkManager.Singleton.StartHost();
            }
            else if (joinedLobby != null)
            {
                NetworkManager.Singleton.StartClient();
            }
        }
    }

    private void OnKick()
    {
        OnKickedFromLobby?.Invoke();
        if(joinedLobby != null)
        {
            hostLobby = null;
            joinedLobby = null;
        }
        StartCoroutine(ShutdownLoadMainMenu());
    }

    private IEnumerator ShutdownLoadMainMenu()
    {
        NetworkManager.Singleton.Shutdown();
        yield return new WaitWhile(() => NetworkManager.Singleton.ShutdownInProgress);
        SceneLoader.Instance.LoadScene("Main Menu");
    }
}
