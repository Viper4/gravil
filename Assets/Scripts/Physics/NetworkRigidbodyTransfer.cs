using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkRigidbodyTransfer : NetworkRigidbody
{
    public bool CanChangeOwnership { get; set; } = true;

    public void ChangeOwnership(ulong clientId)
    {
        if (NetworkObject.IsOwnershipLocked || !CanChangeOwnership)
            return;

        if (clientId != OwnerClientId)
        {
            if (IsServer)
            {
                NetworkObject.ChangeOwnership(clientId);
            }
            else
            {
                ChangeOwnershipRpc(clientId);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void ChangeOwnershipRpc(ulong clientId)
    {
        NetworkObject.ChangeOwnership(clientId);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.rigidbody != null && IsServer)
        {
            if (collision.rigidbody.CompareTag("Player"))
            {
                if (collision.rigidbody.TryGetComponent(out PlayerControl playerControl))
                {
                    ChangeOwnership(playerControl.OwnerClientId);
                }
            }
        }
    }
}
