using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(Gravity))]
public class NetworkGravity : NetworkBehaviour
{
    private Gravity gravity;

    [SerializeField] private NetworkVariable<float> directionX = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<float> directionY = new NetworkVariable<float>(-1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<float> directionZ = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Start()
    {
        gravity = GetComponent<Gravity>();

        gravity.SetDirection(new Vector3(directionX.Value, directionY.Value, directionZ.Value));
        directionX.OnValueChanged += (prevVal, newVal) => OnDirectionChanged();
        directionY.OnValueChanged += (prevVal, newVal) => OnDirectionChanged();
        directionZ.OnValueChanged += (prevVal, newVal) => OnDirectionChanged();
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
        gravity.SetDirection(direction) ;
        directionX.Value = direction.x;
        directionY.Value = direction.y;
        directionZ.Value = direction.z;
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
