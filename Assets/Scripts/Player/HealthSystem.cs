using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class HealthSystem : NetworkBehaviour
{
    [System.Serializable]
    private enum Axis
    {
        X, Y, Z
    }

    [SerializeField] private NetworkVariable<int> health = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<int> maxHealth = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] private Transform healthBarParent;
    [SerializeField] private Transform healthBar;
    [SerializeField] private Axis scaleAxis = Axis.X;
    private Vector3 maxHealthScale;

    /*public struct CustomData : INetworkSerializable
    {
        public int health;
        public int maxHealth;
        public string test;
        public FixedString128Bytes message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref health);
            serializer.SerializeValue(ref maxHealth);
            serializer.SerializeValue(ref test);
        }
    }*/

    public override void OnNetworkSpawn()
    {
        if (healthBar != null)
        {
            maxHealthScale = healthBar.localScale;
            UpdateHealthBar();
        }
    }

    void Update()
    {
        if(Camera.main != null)
        {
            healthBarParent.rotation = Quaternion.LookRotation(Camera.main.transform.forward, transform.up);
        }
    }

    private void UpdateHealthBar()
    {
        if(healthBar != null)
        {
            float healthPercent = health.Value / maxHealth.Value;
            switch (scaleAxis)
            {
                case Axis.X:
                    healthBar.localScale = new Vector3(healthPercent * maxHealthScale.x, healthBar.localScale.y, healthBar.localScale.z);
                    break;
                case Axis.Y:
                    healthBar.localScale = new Vector3(healthBar.localScale.x, healthPercent * maxHealthScale.y, healthBar.localScale.z);
                    break;
                case Axis.Z:
                    healthBar.localScale = new Vector3(healthBar.localScale.x, healthBar.localScale.y, healthPercent * maxHealthScale.z);
                    break;
            }
        }
    }

    public void Damage(int amount)
    {
        if (!IsOwner)
            return;
        health.Value -= amount;
        UpdateHealthBar();
    }

    public void Heal(int amount)
    {
        if (!IsOwner)
            return;
        health.Value += amount;
        UpdateHealthBar();
    }
}
