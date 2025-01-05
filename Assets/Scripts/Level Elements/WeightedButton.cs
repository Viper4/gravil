using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class WeightedButton : MonoBehaviour
{
    [SerializeField] private Transform movingPart;
    [SerializeField] private BoxCollider triggerCollider;

    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 endPosition;
    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private UnityEvent OnStartPress;
    [SerializeField] private UnityEvent OnEndPress;
    private bool endPressed = false;
    [SerializeField] private UnityEvent OnStartRelease;
    [SerializeField] private UnityEvent OnEndRelease;
    private bool endReleased = false;

    private int objectsOnButton = 0;

    [SerializeField] private LayerMask ignoreLayers;

    private void Update()
    {
        if(objectsOnButton > 0)
        {
            if(movingPart.localPosition == endPosition)
            {
                if (!endPressed)
                {
                    OnEndPress?.Invoke();
                    endPressed = true;
                }
            }
            else
            {
                endPressed = false;
                movingPart.localPosition = Vector3.MoveTowards(movingPart.localPosition, endPosition, moveSpeed * Time.deltaTime);
            }
        }
        else
        {
            if (movingPart.localPosition == startPosition)
            {
                if (!endReleased)
                {
                    OnEndRelease?.Invoke();
                    endReleased = true;
                }
            }
            else
            {
                endReleased = false;
                movingPart.localPosition = Vector3.MoveTowards(movingPart.localPosition, startPosition, moveSpeed * Time.deltaTime);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger && other.TryGetComponent<Interactable>(out var otherInteractable) && otherInteractable.canInteract)
        {
            AddObject(otherInteractable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.isTrigger && other.TryGetComponent<Interactable>(out var otherInteractable) && otherInteractable.canInteract)
        {
            RemoveObject(otherInteractable);
        }
    }

    public void AddObject(Interactable interactable)
    {
        interactable.interactedButton = this;
        if (objectsOnButton == 0)
        {
            OnStartPress?.Invoke();
        }
        objectsOnButton++;
    }

    public void RemoveObject(Interactable interactable)
    {
        interactable.interactedButton = null;
        objectsOnButton--;
        if (objectsOnButton < 0)
        {
            objectsOnButton = 0;
        }
        if (objectsOnButton == 0)
        {
            OnStartRelease?.Invoke();
        }
    }
}
