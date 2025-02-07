using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class Gravity : MonoBehaviour
{
    public Rigidbody Rigidbody { get; private set; }

    public float acceleration = 9.81f;
    [SerializeField] private Vector3 direction = Vector3.down;

    [SerializeField] private float terminalVelocity = 53f;

    [SerializeField] private bool canLock = false;
    public bool IsLocked { get; private set; }
    [SerializeField] private int lockOffset = 0;
    private GravityLock gravityLock;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip changeDirectionClip;
    [SerializeField] private AudioClip lockedClip;

    [SerializeField] private Renderer directionIndicator;
    [SerializeField] private Image reticle;
    [SerializeField] private Vector3[] indicatorDirections;
    [SerializeField] private Color[] indicatorColors;

    public bool offline = true;

    public Action<Vector3> OnLockChanged;

    public LayerMask collisionLayers;

    private void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        Rigidbody.useGravity = false;
        UpdateDirectionIndicator();
    }

    private void FixedUpdate()
    {
        if (!Rigidbody.isKinematic)
        {
            if (Vector3.Dot(Rigidbody.linearVelocity, direction) < terminalVelocity)
            {
                Rigidbody.AddForce(direction * acceleration, ForceMode.Acceleration);
            }
        }
    }

    public void SetGravityLock(GravityLock gravityLock)
    {
        this.gravityLock = gravityLock;
        direction = gravityLock.GetDirection(lockOffset);
        UpdateDirectionIndicator();
        gravityLock.OnDirectionChanged += GravityLockDirectionChange;
        IsLocked = true;
    }

    public void RemoveGravityLock()
    {
        if (gravityLock != null)
        {
            gravityLock.OnDirectionChanged -= GravityLockDirectionChange;
            IsLocked = false;
            gravityLock = null;
        }
    }

    public void CheckColliderEnter(Collider other)
    {
        if (!IsLocked && canLock && other.CompareTag("GravityLock") && other.TryGetComponent(out GravityLock gravityLock))
        {
            SetGravityLock(gravityLock);
        }
    }

    public void CheckColliderExit(Collider other)
    {
        if (IsLocked && canLock && other.CompareTag("GravityLock") && other.transform == gravityLock.transform)
        {
            RemoveGravityLock();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (offline)
        {
            CheckColliderEnter(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (offline)
        {
            CheckColliderExit(other);
        }
    }

    private void UpdateDirectionIndicator()
    {
        int closestIndex = 0;
        float closestAngle = Vector3.Angle(indicatorDirections[0], direction);
        for (int i = 1; i < indicatorDirections.Length; i++)
        {
            float angle = Vector3.Angle(indicatorDirections[i], direction);
            if (angle < closestAngle)
            {
                closestAngle = angle;
                closestIndex = i;
            }
        }

        if (directionIndicator != null)
        {
            directionIndicator.material.color = indicatorColors[closestIndex];
        }

        if(reticle != null)
        {
            reticle.color = indicatorColors[closestIndex];
        }
    }

    public Vector3 GetDirection()
    {
        return direction;
    }

    public bool SetDirection(Vector3 direction)
    {
        if (audioSource != null)
            audioSource.PlayOneShot(changeDirectionClip);

        this.direction = direction;

        UpdateDirectionIndicator();
        return true;
    }

    private void GravityLockDirectionChange(int index)
    {
        direction = gravityLock.GetDirection(lockOffset);
        OnLockChanged?.Invoke(direction);
        UpdateDirectionIndicator();
    }

    public void ForceUnlock()
    {
        if (gravityLock != null)
            gravityLock.OnDirectionChanged -= GravityLockDirectionChange;
        IsLocked = false;
        gravityLock = null;
    }

    public void PlayLockedSound()
    {
        if (audioSource != null)
            audioSource.PlayOneShot(lockedClip);
    }
}
