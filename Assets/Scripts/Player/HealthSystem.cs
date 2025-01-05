using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : NetworkBehaviour
{
    [System.Serializable]
    private enum Axis
    {
        X, Y, Z
    }

    [SerializeField] private NetworkVariable<float> health = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private NetworkVariable<float> maxHealth = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] private Transform healthBarParent;
    [SerializeField] private Transform healthBar;
    [SerializeField] private Axis scaleAxis = Axis.X;
    private Vector3 maxHealthScale;

    [SerializeField] private Animator healthAnimator;
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private UnityEvent OnDamage;

    [SerializeField] private float minDamageSpeed = 16.0f;
    [SerializeField] private float speedDamageMultiplier = 2.5f;

    [SerializeField] private UnityEvent OnDeath;

    private void Start()
    {
        if (healthBar != null)
        {
            maxHealthScale = healthBar.localScale;
            UpdateHealthBar();
            health.OnValueChanged += (prevVal, newVal) => UpdateHealthBar();
            maxHealth.OnValueChanged += (prevVal, newVal) => UpdateHealthBar();
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
        if (healthBar != null)
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

    public void Damage(float amount)
    {
        if (!IsOwner)
            return;

        health.Value -= amount;
        healthAnimator.SetTrigger("Damage");
        OnDamage?.Invoke();
        if (health.Value <= 0)
        {
            health.Value = 0;
            OnDeath?.Invoke();
        }
        else if (damageEffect != null)
        {
            Destroy(Instantiate(damageEffect, transform.position, transform.rotation), 5f);
        }
        UpdateHealthBar();
    }

    public void Heal(float amount)
    {
        if (!IsOwner)
            return;

        health.Value += amount;
        UpdateHealthBar();
        healthAnimator.SetTrigger("Heal");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner)
            return;
        float collisionSpeed = collision.relativeVelocity.magnitude;
        if(collisionSpeed >= minDamageSpeed)
        {
            Damage(collisionSpeed * speedDamageMultiplier);
        }
    }

    public void ResetHealth()
    {
        if (!IsOwner)
            return;

        health.Value = maxHealth.Value;
        UpdateHealthBar();
    }
}
