using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Gravity : MonoBehaviour
{
    [HideInInspector] public Rigidbody attachedRB;

    public float acceleration = 9.81f;
    public Vector3 direction = Vector3.down;

    private void Start()
    {
        attachedRB = GetComponent<Rigidbody>();
        attachedRB.useGravity = false;
    }

    private void FixedUpdate()
    {
        if (!attachedRB.isKinematic)
        {
            attachedRB.AddForce(direction * acceleration, ForceMode.Acceleration);
        }
    }
}
