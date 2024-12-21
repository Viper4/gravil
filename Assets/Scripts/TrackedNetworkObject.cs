using Unity.Netcode;
using UnityEngine;

public class TrackedNetworkObject : NetworkBehaviour
{
    private void OnNetworkInstantiate()
    {
        GameManager.Instance.trackedNetworkObjects.Add(NetworkObjectId, NetworkObject);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        GameManager.Instance.trackedNetworkObjects.Remove(NetworkObjectId);
    }
}
