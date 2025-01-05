using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkRigidbodyOwner : NetworkRigidbody
{
    [SerializeField] private bool useTriggers = false;

    private List<NetworkRigidbody> touchingObjects = new List<NetworkRigidbody>();

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        for (int i = 0; i < touchingObjects.Count; i++)
        {
            if(touchingObjects[i] == null)
            {
                touchingObjects.RemoveAt(i);
                i--;
            }
            else
            {
                
            }
        }
    }

    private void OnEnter(Rigidbody otherRB)
    {
        if (!IsOwner || otherRB == Rigidbody)
            return;

        if (otherRB != null && otherRB.TryGetComponent(out NetworkRigidbody otherNetworkRigidbody))
        {
            // Only add non owner objects so we dont double the force of the owner's objects
            if (!otherNetworkRigidbody.IsOwner)
            {
                touchingObjects.Add(otherNetworkRigidbody);
            }
        }
    }

    private void OnExit(Rigidbody otherRB)
    {
        if (!IsOwner || otherRB == Rigidbody)
            return;

        if (otherRB != null && otherRB.TryGetComponent(out NetworkRigidbody otherNetworkRigidbody))
        {
            if (!otherNetworkRigidbody.IsOwner)
            {
                touchingObjects.Remove(otherNetworkRigidbody);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnEnter(collision.rigidbody);
    }

    private void OnCollisionExit(Collision collision)
    {
        OnExit(collision.rigidbody);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (useTriggers)
            OnEnter(other.attachedRigidbody);
    }

    private void OnTriggerExit(Collider other)
    {
        if (useTriggers)
            OnExit(other.attachedRigidbody);
    }
}
