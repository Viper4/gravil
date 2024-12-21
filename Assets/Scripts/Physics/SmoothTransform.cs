using UnityEngine;

public class SmoothTransform : MonoBehaviour
{
    [SerializeField] private Vector3[] positions;
    [SerializeField] private float moveSpeed;
    private int positionIndex = -1;

    [SerializeField] private Vector3[] eulers;
    [SerializeField] private float rotateSpeed;
    private int rotationIndex = -1;

    void Update()
    {
        if (positionIndex > -1)
        {
            if(transform.localPosition != positions[positionIndex])
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, positions[positionIndex], moveSpeed * Time.deltaTime);
            }
            else
            {
                positionIndex = -1;
            }
        }

        if (rotationIndex > -1)
        {
            Quaternion targetRotation = Quaternion.Euler(eulers[rotationIndex]);
            if(Quaternion.Angle(transform.localRotation, targetRotation) > 0.1f)
            {
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, Quaternion.Euler(eulers[rotationIndex]), rotateSpeed * Time.deltaTime);
            }
            else
            {
                rotationIndex = -1;
            }
        }
    }

    public void StartMove(int index)
    {
        if(index < positions.Length)
        {
            positionIndex = index;
        }
    }

    public void StopMove()
    {
        positionIndex = -1;
    }

    public void StartRotate(int index)
    {
        if(index < eulers.Length)
        {
            rotationIndex = index;
        }
    }

    public void StopRotate()
    {
        rotationIndex = -1;
    }
}
