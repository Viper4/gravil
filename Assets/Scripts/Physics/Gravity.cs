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

    public bool IsLocked { get; private set; }
    [SerializeField] private int lockOffset = 0;
    [SerializeField] private bool canLock = true;
    private GravityLock gravityLock;
    private GravityLock previousGravityLock;

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

    private float timeSinceSwitch = 0f;

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
        if (!canLock)
            return;

        previousGravityLock = this.gravityLock;
        this.gravityLock = gravityLock;
        direction = gravityLock.GetDirection(lockOffset);
        UpdateDirectionIndicator();
        gravityLock.OnDirectionChanged += GravityLockDirectionChange;
        OnLockChanged?.Invoke(direction);
        IsLocked = true;
    }

    public void RemoveGravityLock()
    {
        if (gravityLock != null)
        {
            gravityLock.OnDirectionChanged -= GravityLockDirectionChange;
            IsLocked = false;
            gravityLock = null;
            if (previousGravityLock != null)
            {
                SetGravityLock(previousGravityLock);
            }
        }
    }

    public void TryRemoveGravityLock(GravityLock gravityLock)
    {
        if (this.gravityLock == gravityLock)
        {
            RemoveGravityLock();
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

        if (reticle != null)
        {
            reticle.color = indicatorColors[closestIndex];
        }
    }

    public Vector3 GetDirection()
    {
        return direction;
    }

    public void SetDirection(Vector3 direction)
    {
        if (audioSource != null && this.direction != direction && Time.time - timeSinceSwitch > 0.01f)
        {
            audioSource.PlayOneShot(changeDirectionClip);
            timeSinceSwitch = Time.time;
        }

        this.direction = direction;

        UpdateDirectionIndicator();
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
