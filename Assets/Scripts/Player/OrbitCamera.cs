using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float dstFromTarget = 4;

    [SerializeField] private float rotateSpeed = 5f;

    void Update()
    {
        transform.rotation *= Quaternion.AngleAxis(rotateSpeed * Time.deltaTime, Vector3.up);
        transform.position = target.position - transform.forward * dstFromTarget;
    }
}
