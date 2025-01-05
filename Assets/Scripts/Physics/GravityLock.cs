using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GravityLock : MonoBehaviour
{
    [SerializeField] private Vector3[] directions;
    [SerializeField] private int directionIndex = 0;
    private Vector3 direction;

    public Action<int> OnDirectionChanged;

    private void Start()
    {
        direction = directions[directionIndex];
    }

    public Vector3 GetDirection()
    {
        return direction;
    }

    public Vector3 GetDirection(int offset)
    {
        int offsetIndex = directionIndex + offset;
        if (offsetIndex < 0)
        {
            offsetIndex = directions.Length - 1;
        }
        else if (offsetIndex >= directions.Length)
        {
            offsetIndex = 0;
        }
        return directions[offsetIndex];
    }

    public void SetDirection(int index)
    {
        if (index < 0 || index >= directions.Length)
            return;
        directionIndex = index;
        direction = directions[index];
        OnDirectionChanged?.Invoke(index);
    }

    public void IncrementDirection(int amount)
    {
        directionIndex += amount;
        if(directionIndex < 0)
        {
            directionIndex = directions.Length - 1;
        }
        else if(directionIndex >= directions.Length)
        {
            directionIndex = 0;
        }
        direction = directions[directionIndex];
        OnDirectionChanged?.Invoke(directionIndex);
    }
}
