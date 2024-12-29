using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

public class NetworkRigidbodyOwner : NetworkRigidbody
{
    [SerializeField] private bool useTriggers = false;

    private List<NetworkGravity> touchingObjects = new List<NetworkGravity>();

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
                // Check if we are going to collide with this object
                Vector3 collisionDirection = touchingObjects[i].transform.position - transform.position;

                float dot = Vector3.Dot(touchingObjects[i].direction, collisionDirection);
                Debug.Log(name + " touching " + touchingObjects[i].name + ": " + dot);
                if (dot < -0.05f)
                {
                    Rigidbody.AddForce(touchingObjects[i].attachedRigidbody.mass * touchingObjects[i].acceleration * touchingObjects[i].direction, ForceMode.Force);
                }
            }
        }
    }

    private void OnEnter(Rigidbody otherRB)
    {
        if (!IsOwner || otherRB == Rigidbody)
            return;

        if (otherRB != null && otherRB.TryGetComponent(out NetworkGravity otherNetworkGravity))
        {
            // Only add non owner objects so we dont double the force of the owner's objects
            if (!otherNetworkGravity.IsOwner)
            {
                touchingObjects.Add(otherNetworkGravity);
            }
        }
    }

    private void OnExit(Rigidbody otherRB)
    {
        if (!IsOwner || otherRB == Rigidbody)
            return;

        if (otherRB != null && otherRB.TryGetComponent(out NetworkGravity otherNetworkGravity))
        {
            if (!otherNetworkGravity.IsOwner)
            {
                touchingObjects.Remove(otherNetworkGravity);
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
