using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NetworkGravity : NetworkBehaviour
{
    public Rigidbody attachedRigidbody { get; private set; }

    public float acceleration = 9.81f;
    public Vector3 direction { get; private set; } = Vector3.down;
    [SerializeField] private NetworkVariable<float> directionX = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<float> directionY = new NetworkVariable<float>(-1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<float> directionZ = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] private float terminalVelocity = 53f;

    private void Start()
    {
        attachedRigidbody = GetComponent<Rigidbody>();
        attachedRigidbody.useGravity = false;

        direction = new Vector3(directionX.Value, directionY.Value, directionZ.Value);
        directionX.OnValueChanged += (prevVal, newVal) => OnDirectionChanged();
        directionY.OnValueChanged += (prevVal, newVal) => OnDirectionChanged();
        directionZ.OnValueChanged += (prevVal, newVal) => OnDirectionChanged();
    }

    private void FixedUpdate()
    {
        if (!attachedRigidbody.isKinematic)
        {
            if(Vector3.Dot(attachedRigidbody.linearVelocity, direction) < terminalVelocity)
            {
                attachedRigidbody.AddForce(direction * acceleration, ForceMode.Acceleration);
            }
        }
    }

    private void OnDirectionChanged()
    {
        direction = new Vector3(directionX.Value, directionY.Value, directionZ.Value);
    }

    public void SetDirection(Vector3 newDirection)
    {
        direction = newDirection;
        directionX.Value = newDirection.x;
        directionY.Value = newDirection.y;
        directionZ.Value = newDirection.z;
    }
}
