using Unity.Netcode.Components;
using UnityEngine;

public class NetworkRigidbodyClient : NetworkRigidbody
{
    private void TryOwnershipTransfer(Rigidbody other)
    {
        if (other != null && IsServer)
        {
            if (other.CompareTag("Player"))
            {
                PlayerControl playerControl = other.GetComponent<PlayerControl>();
                if (playerControl.OwnerClientId != OwnerClientId)
                {
                    NetworkObject.ChangeOwnership(playerControl.OwnerClientId);
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryOwnershipTransfer(collision.rigidbody);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryOwnershipTransfer(other.attachedRigidbody);
    }
}
