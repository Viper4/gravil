using NUnit.Framework;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkGravity))]
public class NetworkRigidbodyServer : NetworkRigidbody
{
    private NetworkGravity gravity;

    private List<NetworkGravity> touchingObjects = new List<NetworkGravity>();

    private void Start()
    {
        gravity = GetComponent<NetworkGravity>();
    }

    private void FixedUpdate()
    {
        if (!IsServer)
            return;

        foreach (NetworkGravity otherGravity in touchingObjects)
        {
            gravity.attachedRB.AddForce(otherGravity.acceleration * otherGravity.attachedRB.mass * otherGravity.direction, ForceMode.Acceleration);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer)
            return;

        if (collision.rigidbody != null && collision.rigidbody.TryGetComponent(out NetworkRigidbody otherNetworkRB))
        {
            // Only add client objects so we dont double the force of the server's objects
            if (!otherNetworkRB.IsOwner)
            {
                Rigidbody.AddForceAtPosition(collision.relativeVelocity * otherNetworkRB.Rigidbody.mass, collision.GetContact(0).point, ForceMode.Impulse);
                if (otherNetworkRB.TryGetComponent(out NetworkGravity otherGravity))
                {
                    touchingObjects.Add(otherGravity);
                }
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!IsServer)
            return;
        if (collision.rigidbody != null && collision.rigidbody.TryGetComponent(out NetworkRigidbody otherNetworkRB))
        {
            if (!otherNetworkRB.IsOwner)
            {
                if (otherNetworkRB.TryGetComponent(out NetworkGravity otherGravity))
                {
                    touchingObjects.Remove(otherGravity);
                }
            }
        }
    }
}
