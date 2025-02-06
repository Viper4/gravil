using System;
using System.Linq;
using Unity.Services.Vivox;
using UnityEngine;
using System.Collections.Generic;
using Unity.Services.Core;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    public event Action OnSelfMuted;
    public event Action OnSelfUnmuted;
    public event Action<string> OnMutePlayer;
    public event Action<string> OnUnmutePlayer;

    private string currentChannelName;

    [SerializeField] private GameObject connectionUI;

    public Dictionary<string, VivoxParticipant> participants = new Dictionary<string, VivoxParticipant>();

    private async void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAddedToChannel;
        VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantRemovedFromChannel;

        await VivoxService.Instance.InitializeAsync();

        LobbyManager.Instance.OnJoinLobby += ctx => JoinVoiceGroup(ctx);
        LobbyManager.Instance.OnKickedFromLobby += () => LeaveVoiceGroup();
    }

    public async void LoginToVivoxAsync()
    {
        LoginOptions options = new LoginOptions();
        options.DisplayName = LobbyManager.Instance.playerName;
        options.EnableTTS = true;
        await VivoxService.Instance.LoginAsync(options);
    }

    public async void LogoutOfVivoxAsync()
    {
        await VivoxService.Instance.LogoutAsync();
    }

    private async void JoinVoiceGroup(string channelName)
    {
        ChannelOptions options = new ChannelOptions();
        options.MakeActiveChannelUponJoining = true;
        connectionUI.SetActive(true);
        await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.AudioOnly, options);
        connectionUI.SetActive(false);
        currentChannelName = channelName;
        participants.Clear();
        foreach (VivoxParticipant participant in VivoxService.Instance.ActiveChannels[currentChannelName])
        {
            if(participants.ContainsKey(participant.PlayerId))
                continue;
            participants.Add(participant.PlayerId, participant);
            OnUnmutePlayer?.Invoke(participant.PlayerId);
        }
    }

    private async void LeaveVoiceGroup()
    {
        await VivoxService.Instance.LeaveChannelAsync(currentChannelName);
        currentChannelName = null;
        participants.Clear();
    }

    private void OnParticipantAddedToChannel(VivoxParticipant participant)
    {
        participants.Add(participant.PlayerId, participant);
    }

    private void OnParticipantRemovedFromChannel(VivoxParticipant participant)
    {
        participants.Remove(participant.PlayerId);
    }

    public async void MuteSelf()
    {
        await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.None, currentChannelName);
        OnSelfMuted?.Invoke();
    }

    public async void UnmuteSelf()
    {
        await VivoxService.Instance.SetChannelTransmissionModeAsync(TransmissionMode.All, currentChannelName);
        OnSelfUnmuted?.Invoke();
    }

    public void MutePlayerLocally(string playerId)
    {
        VivoxService.Instance.ActiveChannels[currentChannelName].Where(participant => participant.PlayerId == playerId).First().MutePlayerLocally();
        OnMutePlayer?.Invoke(playerId);
    }

    public void UnmutePlayerLocally(string playerId)
    {
        VivoxService.Instance.ActiveChannels[currentChannelName].Where(participant => participant.PlayerId == playerId).First().UnmutePlayerLocally();
        OnUnmutePlayer?.Invoke(playerId);
    }
}
