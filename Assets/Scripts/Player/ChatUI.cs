using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ChatUI : MonoBehaviour
{
    [SerializeField] private PlayerControl attachedPlayer;

    private bool muted;
    [SerializeField] private Image chatImage;
    [SerializeField] private Sprite selfMutedIcon;
    [SerializeField] private Sprite mutedIcon;

    [SerializeField] private Sprite[] speakingIcons;
    [SerializeField] private Color[] speakingColors;

    private void Start()
    {
        if (attachedPlayer.IsOwner)
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
            chatImage.sprite = speakingIcons[0];
            chatImage.color = speakingColors[0];
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
        if (playerId == attachedPlayer.playerId)
        {
            chatImage.sprite = mutedIcon;
            chatImage.color = Color.red;
            muted = true;
        }
    }

    private void OnUnmutePlayer(string playerId)
    {
        if (playerId == attachedPlayer.playerId)
        {
            muted = false;
        }
    }
}
