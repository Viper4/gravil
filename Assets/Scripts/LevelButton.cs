using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class LevelButton : MonoBehaviour
{
    [SerializeField] private Transform movingPart;

    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 endPosition;
    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private UnityEvent OnStartPress;
    [SerializeField] private UnityEvent OnEndPress;
    [SerializeField] private UnityEvent OnStartRelease;
    [SerializeField] private UnityEvent OnEndRelease;

    [SerializeField] private string[] interactTags;
    private HashSet<string> tagsSet;
    private int objectsOnButton = 0;

    private void Start()
    {
        tagsSet = new HashSet<string>(interactTags);
        foreach(string tag in interactTags)
        {
            tagsSet.Add(tag);
        }
    }

    private void Update()
    {
        if(objectsOnButton > 0)
        {
            if(movingPart.localPosition == endPosition)
            {
                OnEndPress?.Invoke();
            }
            else
            {
                movingPart.localPosition = Vector3.MoveTowards(movingPart.localPosition, endPosition, moveSpeed * Time.deltaTime);
            }
        }
        else
        {
            if (movingPart.localPosition == startPosition)
            {
                OnEndRelease?.Invoke();
            }
            else
            {
                movingPart.localPosition = Vector3.MoveTowards(movingPart.localPosition, startPosition, moveSpeed * Time.deltaTime);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger && tagsSet.Contains(other.tag))
        {
            if (objectsOnButton == 0)
            {
                OnStartPress?.Invoke();
            }
            objectsOnButton++;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.isTrigger && tagsSet.Contains(other.tag))
        {
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
}
