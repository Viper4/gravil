using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(Gravity))]
public class NetworkGravity : NetworkBehaviour
{
    public Gravity gravity { get; private set; }

    [SerializeField] private NetworkVariable<float> directionX = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<float> directionY = new NetworkVariable<float>(-1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<float> directionZ = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void OnEnable()
    {
        gravity = GetComponent<Gravity>();

        gravity.OnLockChanged += SetNetworkDirection;
    }

    private void OnDisable()
    {
        gravity.OnLockChanged -= SetNetworkDirection;
    }

    private void Start()
    {
        gravity.offline = false;
        gravity.SetDirection(new Vector3(directionX.Value, directionY.Value, directionZ.Value));
        directionX.OnValueChanged += (prevVal, newVal) => OnDirectionChanged();
        directionY.OnValueChanged += (prevVal, newVal) => OnDirectionChanged();
        directionZ.OnValueChanged += (prevVal, newVal) => OnDirectionChanged();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner)
            return;

        gravity.CheckColliderEnter(other);
        Vector3 newDirection = gravity.GetDirection();
        SetNetworkDirection(newDirection);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsOwner)
            return;

       gravity.CheckColliderExit(other);
    }

    private void SetNetworkDirection(Vector3 direction)
    {
        directionX.Value = direction.x;
        directionY.Value = direction.y;
        directionZ.Value = direction.z;
    }

    private void OnDirectionChanged()
    {
        gravity.SetDirection(new Vector3(directionX.Value, directionY.Value, directionZ.Value));
    }

    public Vector3 GetDirection()
    {
        return gravity.GetDirection();
    }

    public void SetDirection(Vector3 direction)
    {
        if (!IsOwner)
        {
            RequestSetDirectionRpc(direction);
        }
        else 
        {
            if (gravity.SetDirection(direction))
            {
                directionX.Value = direction.x;
                directionY.Value = direction.y;
                directionZ.Value = direction.z;
            }
        }
    }

    [Rpc(SendTo.Owner)]
    private void RequestSetDirectionRpc(Vector3 direction)
    {
        SetDirection(direction);
    }

    public float GetAcceleration()
    {
        return gravity.acceleration;
    }

    public void SetAcceleration(float newAcceleration)
    {
        gravity.acceleration = newAcceleration;
    }

    public void ForceUnlock()
    {
        gravity.ForceUnlock();
    }
}
