using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Gravity : MonoBehaviour
{
    [HideInInspector] public Rigidbody attachedRigidbody;

    public float acceleration = 9.81f;
    public Vector3 direction = Vector3.down;

    [SerializeField] private float terminalVelocity = 53f;

    private void Start()
    {
        attachedRigidbody = GetComponent<Rigidbody>();
        attachedRigidbody.useGravity = false;
    }

    private void FixedUpdate()
    {
        if (!attachedRigidbody.isKinematic)
        {
            if (Vector3.Dot(attachedRigidbody.linearVelocity, direction) < terminalVelocity)
            {
                attachedRigidbody.AddForce(direction * acceleration, ForceMode.Acceleration);
            }
        }
    }
}
