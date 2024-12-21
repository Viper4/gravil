using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameObject networkManager;

    public Dictionary<ulong, NetworkObject> trackedNetworkObjects = new Dictionary<ulong, NetworkObject>();

    private void OnEnable()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Instantiate(networkManager);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
