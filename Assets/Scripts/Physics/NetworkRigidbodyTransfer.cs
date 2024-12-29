using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkRigidbodyTransfer : NetworkRigidbody
{
    public bool canChangeOwnership = true;

    public void ChangeOwnership(ulong clientId)
    {
        if (!IsServer || NetworkObject.IsOwnershipLocked || !canChangeOwnership)
            return;

        if (clientId != OwnerClientId)
        {
            NetworkObject.ChangeOwnership(clientId);
        }
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
