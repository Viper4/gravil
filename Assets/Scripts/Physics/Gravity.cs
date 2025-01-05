using UnityEngine;

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

    [SerializeField] private MeshRenderer directionIndicator;
    [SerializeField] private Vector3[] indicatorDirections;
    [SerializeField] private Color[] indicatorColors;

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

    private void OnTriggerEnter(Collider other)
    {
        if (canLock && other.CompareTag("GravityLock") && other.TryGetComponent(out GravityLock gravityLock))
        {
            this.gravityLock = gravityLock;
            direction = gravityLock.GetDirection(lockOffset);
            UpdateDirectionIndicator();
            gravityLock.OnDirectionChanged += GravityLockDirectionChange;
            IsLocked = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (canLock && other.CompareTag("GravityLock") && other.TryGetComponent(out GravityLock gravityLock))
        {
            gravityLock.OnDirectionChanged -= GravityLockDirectionChange;
            IsLocked = false;
            this.gravityLock = null;
        }
    }

    private void UpdateDirectionIndicator()
    {
        if (directionIndicator != null)
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

            directionIndicator.material.color = indicatorColors[closestIndex];
        }
    }

    public Vector3 GetDirection()
    {
        return direction;
    }

    public void SetDirection(Vector3 direction)
    {
        if (IsLocked)
        {
            if(audioSource != null)
                audioSource.PlayOneShot(lockedClip);
            return;
        }

        if (audioSource != null)
            audioSource.PlayOneShot(changeDirectionClip);

        this.direction = direction;

        UpdateDirectionIndicator();
    }

    private void GravityLockDirectionChange(int index)
    {
        direction = gravityLock.GetDirection(lockOffset);
        UpdateDirectionIndicator();
    }

    public void ForceUnlock()
    {
        gravityLock.OnDirectionChanged -= GravityLockDirectionChange;
        IsLocked = false;
        gravityLock = null;
    }
}
