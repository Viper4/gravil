using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class SmoothTransform : NetworkBehaviour
{
    [SerializeField] private bool worldSpace = false;

    public Vector3[] positions;
    [SerializeField] private float moveSpeed;
    [SerializeField] private int positionIndex = 0;
    private bool moving = false;

    public Vector3[] eulers;
    [SerializeField] private float rotateSpeed;
    [SerializeField] private int rotationIndex = 0;
    private bool rotating = false;

    [SerializeField] private bool allowRiders = false;

    private bool reachedPosition = true;
    public UnityEvent<int> OnPositionReached;
    private bool reachedRotation = true;
    public UnityEvent<int> OnRotationReached;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private void Update()
    {
        if (!IsOwner)
            return;

        if (moving)
        {
            if (worldSpace)
            {
                if (transform.position == targetPosition)
                {
                    transform.position = targetPosition;
                    if (!reachedPosition)
                        ReachedPosition();
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                    reachedPosition = false;
                }
            }
            else
            {
                if (transform.localPosition == targetPosition)
                {
                    transform.localPosition = targetPosition;
                    if (!reachedPosition)
                        ReachedPosition();
                }
                else
                {
                    transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, moveSpeed * Time.deltaTime);
                    reachedPosition = false;
                }
            }
        }

        if (rotating)
        {
            if (worldSpace)
            {
                if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
                {
                    transform.rotation = targetRotation;
                    if (!reachedRotation)
                        ReachedRotation();
                }
                else
                {
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
                    reachedRotation = false;
                }
            }
            else
            {
                if (Quaternion.Angle(transform.localRotation, targetRotation) < 0.1f)
                {
                    transform.localRotation = targetRotation;
                    if (!reachedRotation)
                        ReachedRotation();
                }
                else
                {
                    transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
                    reachedRotation = false;
                }
            }
        }
    }

    private void ReachedPosition()
    {
        moving = false;
        reachedPosition = true;
        OnPositionReached?.Invoke(positionIndex);
    }

    private void ReachedRotation()
    {
        rotating = false;
        reachedRotation = true;
        OnRotationReached?.Invoke(rotationIndex);
    }

    public void SetPosition(int index)
    {
        if (!IsOwner)
            return;

        if (index < positions.Length)
        {
            positionIndex = index;
            if (index >= 0)
            {
                transform.localPosition = positions[index];
            }
        }
    }

    public void SetRotation(int index)
    {
        if (!IsOwner)
            return;

        if (index < eulers.Length)
        {
            rotationIndex = index;
            if(index >= 0)
            {
                transform.localRotation = Quaternion.Euler(eulers[index]);
            }
        }
    }

    public void StartMove(int index)
    {
        if (!IsOwner)
            return;

        moving = true;
        positionIndex = Mathf.Clamp(index, 0, positions.Length - 1);
        targetPosition = positions[positionIndex];
    }

    public void StartMove(Vector3 targetPosition)
    {
        this.targetPosition = targetPosition;
        moving = true;
    }

    public void StopMove()
    {
        if (!IsOwner)
            return;

        moving = false;
    }

    public void StartRotate(int index)
    {
        if (!IsOwner)
            return;

        rotating = true;
        rotationIndex = Mathf.Clamp(index, 0, eulers.Length - 1);
        targetRotation = Quaternion.Euler(eulers[rotationIndex]);
    }

    public void StartRotate(Quaternion targetRotation)
    {
        this.targetRotation = targetRotation;
        rotating = true;
    }

    public void StopRotate()
    {
        if (!IsOwner)
            return;

        rotating = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;

        if (allowRiders && !other.isTrigger && other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic)
        {
            other.attachedRigidbody.transform.SetParent(transform);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer)
            return;

        if (allowRiders && !other.isTrigger && other.attachedRigidbody != null && !other.attachedRigidbody.isKinematic)
        {
            other.attachedRigidbody.transform.SetParent(null);
        }
    }

    public void IncrementMove(int amount)
    {
        if (!IsOwner)
            return;

        StartMove(positionIndex + amount);
    }

    public void IncrementRotate(int amount)
    {
        if (!IsOwner)
            return;

        StartRotate(rotationIndex + amount);
    }
}
