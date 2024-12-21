using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NetworkGravity : NetworkBehaviour
{
    [HideInInspector] public Rigidbody attachedRB;

    public float acceleration = 9.81f;
    public Vector3 direction { get; private set; } = Vector3.down;
    [SerializeField] private NetworkVariable<float> directionX = new NetworkVariable<float>(0f);
    [SerializeField] private NetworkVariable<float> directionY = new NetworkVariable<float>(-1f);
    [SerializeField] private NetworkVariable<float> directionZ = new NetworkVariable<float>(0f);

    private void Start()
    {
        attachedRB = GetComponent<Rigidbody>();
        attachedRB.useGravity = false;

        direction = new Vector3(directionX.Value, directionY.Value, directionZ.Value);
        directionX.OnValueChanged += (prevVal, newVal) => OnDirectionChanged();
        directionY.OnValueChanged += (prevVal, newVal) => OnDirectionChanged();
        directionZ.OnValueChanged += (prevVal, newVal) => OnDirectionChanged();
    }

    private void FixedUpdate()
    {
        if (!attachedRB.isKinematic)
        {
            attachedRB.AddForce(direction * acceleration, ForceMode.Acceleration);
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
