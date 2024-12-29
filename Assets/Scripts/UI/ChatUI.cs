using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using System;
using Unity.Services.Vivox;

public class ChatUI : MonoBehaviour
{
    [SerializeField] private PlayerControl thisPlayer;

    private bool muted;
    [SerializeField] private Image chatImage;
    [SerializeField] private Sprite selfMutedIcon;
    [SerializeField] private Sprite mutedIcon;

    [SerializeField] private Sprite silentIcon;
    [SerializeField] private Sprite[] speakingIcons;
    [SerializeField] private Color[] speakingColors;
    [SerializeField] private float maxAudioEnergy = 0.6f;
    private int speakingIndex = 0;
    private double previousEnergy = -1;

    private void Start()
    {
        if (thisPlayer.IsOwner)
        {
            ChatManager.Instance.OnSelfMuted += SelfMute;
            ChatManager.Instance.OnSelfUnmuted += SelfUnmute;
        }
        ChatManager.Instance.OnMutePlayer += OnMutePlayer;
        ChatManager.Instance.OnUnmutePlayer += OnUnmutePlayer;
    }

    private void Update()
    {
        if (!muted)
        {
            if (ChatManager.Instance.participants.TryGetValue(thisPlayer.playerId, out VivoxParticipant participant))
            {
                double currentEnergy = participant.AudioEnergy;
                if(currentEnergy <= 0.01)
                {
                    chatImage.sprite = silentIcon;
                    chatImage.color = Color.black;
                }
                else
                {
                    if (Math.Abs(previousEnergy - currentEnergy) > 0.01)
                    {
                        previousEnergy = currentEnergy;
                        speakingIndex = 0;
                        float energyInterval = maxAudioEnergy / speakingIcons.Length;
                        for (int i = 0; i < speakingIcons.Length; i++)
                        {
                            if (energyInterval * (i + 1) > currentEnergy)
                            {
                                break;
                            }
                            speakingIndex = i;
                        }
                    }
                    chatImage.sprite = speakingIcons[speakingIndex];
                    chatImage.color = speakingColors[speakingIndex];
                }
            }
        }
    }

    private void SelfMute()
    {
        chatImage.sprite = selfMutedIcon;
        chatImage.color = Color.red;
        muted = true;
    }

    private void SelfUnmute()
    {
        muted = false;
    }

    private void OnMutePlayer(string playerId)
    {
        if (playerId == thisPlayer.playerId)
        {
            chatImage.sprite = mutedIcon;
            chatImage.color = Color.red;
            muted = true;
        }
    }

    private void OnUnmutePlayer(string playerId)
    {
        if (playerId == thisPlayer.playerId)
        {
            muted = false;
        }
    }
}
