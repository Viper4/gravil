using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private int playersInTrigger = 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger && other.CompareTag("Player"))
        {
            playersInTrigger++;
            if (PlayerControl.Instance.IsServer && playersInTrigger >= LobbyManager.Instance.joinedLobby.Players.Count)
            {
                LobbyManager.Instance.LoadNextLevel();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.isTrigger && other.CompareTag("Player"))
        {
            playersInTrigger--;
        }
    }
}
