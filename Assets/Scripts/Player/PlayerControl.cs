using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.Events;

public class PlayerControl : NetworkBehaviour
{
    public static PlayerControl Instance { get; private set; }

    public Rigidbody Rigidbody { get; private set; }
    public NetworkGravity networkGravity;
    public Transform playerModel;
    private CapsuleCollider playerCapsule;
    public Interactable interactable;
    [SerializeField] private GameObject[] localOwnedObjects;
    [SerializeField] private MeshRenderer bodyRenderer;
    public MeshRenderer[] faceRenderers;
    [SerializeField] private TextMeshProUGUI nameText;

    public string playerId;
    public bool IsPaused { get; private set; }

    private Vector2 moveInput;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpVelocity = 2f;
    private Transform ground;
    [HideInInspector] public bool isGrounded = false;
    [SerializeField] private float maxLeanAngle = 15f;
    [SerializeField] private float leanSpeed = 10f;
    [SerializeField] private float gravityRotateSpeed = 125f;

    [SerializeField] private LayerMask hoverLayers;
    public Transform HoveredObject { get; private set; }

    [SerializeField] private float landSpeedThreshold = 0.5f;
    public UnityEvent OnStartMove;
    public UnityEvent OnStopMove;
    public UnityEvent OnJump;
    public UnityEvent OnLand;
    public UnityEvent<string> OnGroundChanged;

    public bool isDead = false;
    [SerializeField] private float deathTime = 1f;
    [SerializeField] private GameObject dieEffectPrefab;
    public bool canRespawn = true;
    public Action OnDeath;
    public Action OnRespawn;

    private void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        playerCapsule = playerModel.GetComponent<CapsuleCollider>();

        if (IsOwner)
        {
            if (Instance == null)
            {
                Instance = this;

                if (!IsServer)
                {
                    // Send my data to everyone else
                    UpdatePlayerLocal(LobbyManager.Instance.playerName, LobbyManager.Instance.playerNameColor, LobbyManager.Instance.playerColor, AuthenticationService.Instance.PlayerId);
                    SendPlayerDataRpc(LobbyManager.Instance.playerName, LobbyManager.Instance.playerNameColor, LobbyManager.Instance.playerColor, AuthenticationService.Instance.PlayerId);
                    RequestPlayerDataRpc(); // Request host sends everyone's data to this player
                }
                else
                {
                    UpdatePlayerLocal(LobbyManager.Instance.playerName, LobbyManager.Instance.playerNameColor, LobbyManager.Instance.playerColor, AuthenticationService.Instance.PlayerId);
                }
                GameManager.Instance.players.Clear();
                GameManager.Instance.players.Add(AuthenticationService.Instance.PlayerId, this);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            for (int i = 0; i < localOwnedObjects.Length; i++)
            {
                Destroy(localOwnedObjects[i]);
            }
        }
    }

    public void SetPaused(bool value)
    {
        IsPaused = value;
        if (IsPaused)
        {
            OnStopMove?.Invoke();
            moveInput = Vector2.zero;
            SendMoveRpc(Vector3.zero);
            SendPositionRpc(transform.position);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.visible = false;
        }
    }

    private void GetHover()
    {
        if (Camera.main == null || isDead)
            return;

        // Raycast to get what the player is hovering over
        Vector3 screenPosition = GameManager.Instance.inputActions.UI.Point.ReadValue<Vector2>();
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hoverHit, Mathf.Infinity, hoverLayers))
        {
            Debug.DrawLine(ray.origin, hoverHit.point, Color.red, 0.1f);
            HoveredObject = hoverHit.collider.transform;
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction, Color.green, 0.1f);
            HoveredObject = null;
        }
    }

    private void SwitchGravity()
    {
        if (isDead)
            return;

        if (networkGravity.gravity.IsLocked)
        {
            networkGravity.gravity.PlayLockedSound();
            return;
        }

        SendPositionRpc(transform.position);

        Vector3 newDirection = Vector3.zero;
        Vector3 cameraForward = Camera.main.transform.forward;

        // Flattening camera forward vector to get a cardinal direction
        if (Mathf.Abs(cameraForward.x) > Mathf.Abs(cameraForward.y))
        {
            if (Mathf.Abs(cameraForward.x) > Mathf.Abs(cameraForward.z))
            {
                newDirection.x = cameraForward.x < 0 ? -1f : 1f;
            }
            else
            {
                newDirection.z = cameraForward.z < 0 ? -1f : 1f;
            }
        }
        else
        {
            if (Mathf.Abs(cameraForward.y) > Mathf.Abs(cameraForward.z))
            {
                newDirection.y = cameraForward.y < 0 ? -1f : 1f;
            }
            else
            {
                newDirection.z = cameraForward.z < 0 ? -1f : 1f;
            }
        }

        networkGravity.SetDirection(newDirection);
    }

    private void Update()
    {
        if (Camera.main == null || !IsSpawned)
            return;

        if (IsOwner)
        {
            if (GameManager.Instance.inputActions.UI.Menu.triggered)
            {
                SetPaused(!IsPaused);
            }

            GetHover();
            if (GameManager.Instance.inputActions.Player.Gravity.triggered)
            {
                SwitchGravity();
            }
        }

        // Rotate player to align with networkGravity
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, -networkGravity.GetDirection()) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, gravityRotateSpeed * Time.deltaTime);

        // Rotate player model to lean with movement
        playerModel.localEulerAngles = new Vector3(playerModel.localEulerAngles.x, Camera.main.transform.localEulerAngles.y, playerModel.localEulerAngles.z);
        Vector3 lean = new Vector3(moveInput.y * maxLeanAngle, Camera.main.transform.localEulerAngles.y, -moveInput.x * maxLeanAngle);
        playerModel.localRotation = Quaternion.RotateTowards(playerModel.localRotation, Quaternion.Euler(lean), leanSpeed * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (!IsSpawned || isDead)
            return;

        Vector3 gravityDirection = networkGravity.GetDirection();
        Vector3 moveDirection;

        if (!IsPaused)
        {
            if (IsOwner)
            {
                Vector2 newMoveInput = GameManager.Instance.inputActions.Player.Move.ReadValue<Vector2>();

                if (moveInput == Vector2.zero && newMoveInput != Vector2.zero)
                {
                    OnStartMove?.Invoke();
                }
                else if (moveInput != Vector2.zero && newMoveInput == Vector2.zero)
                {
                    OnStopMove?.Invoke();
                }

                if (moveInput != newMoveInput)
                {
                    // Sometimes collision detection doesn't work
                    if (!isGrounded && Physics.Raycast((transform.position + transform.up), -transform.up, out RaycastHit groundHit, 1.1f, networkGravity.gravity.collisionLayers, QueryTriggerInteraction.Ignore))
                    {
                        Debug.DrawLine(transform.position, transform.position - (transform.up * 0.1f), Color.red, 0.25f);

                        ground = groundHit.collider.transform;
                        isGrounded = true;
                        OnGroundChanged?.Invoke(ground.tag);
                    }

                    SendMoveRpc(newMoveInput);
                    SendPositionRpc(transform.position);
                }
                moveInput = newMoveInput;

                if (isGrounded && GameManager.Instance.inputActions.Player.Jump.ReadValue<float>() > 0)
                {
                    SendPositionRpc(transform.position);
                    SendJumpRpc();
                    OnJump?.Invoke();
                    Rigidbody.linearVelocity -= gravityDirection * jumpVelocity;
                    isGrounded = false;
                    ground = null;
                    OnGroundChanged?.Invoke("Air");
                }
            }
        }

        moveDirection = transform.forward * moveInput.y + transform.right * moveInput.x;
        Vector3 verticalVelocity = Vector3.Project(Rigidbody.linearVelocity, gravityDirection);
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(moveDirection, gravityDirection).normalized * moveSpeed;
        Vector3 goalVelocity = verticalVelocity + horizontalVelocity;
        Vector3 acceleration = (goalVelocity - Rigidbody.linearVelocity) / Time.fixedDeltaTime;
        Rigidbody.AddForce(acceleration, ForceMode.Acceleration);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner)
            return;

        SendPositionRpc(transform.position);

        if (Vector3.Angle(collision.GetContact(0).normal, -networkGravity.GetDirection()) < 45f)
        {
            ground = collision.collider.transform;
            isGrounded = true;
            OnGroundChanged?.Invoke(ground.tag);
            if (collision.relativeVelocity.magnitude > landSpeedThreshold)
            {
                OnLand?.Invoke();
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!IsOwner)
            return;

        if (collision.collider.transform == ground)
        {
            ground = null;
            isGrounded = false;
            OnGroundChanged?.Invoke("Air");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsOwner)
            return;

        switch (other.tag)
        {
            case "DeathZone":
                Die();
                break;
        }
    }

    public void UpdatePlayerLocal(string name, Color nameColor, Color bodyColor, string playerId)
    {
        this.name = name;
        nameText.text = name;
        nameText.color = nameColor;
        bodyRenderer.material.color = bodyColor;
        foreach (MeshRenderer meshRenderer in faceRenderers)
        {
            meshRenderer.material.color = nameColor;
        }
        this.playerId = playerId;
    }

    [Rpc(SendTo.NotMe)]
    public void SendPlayerDataRpc(string name, Color nameColor, Color bodyColor, string playerId)
    {
        UpdatePlayerLocal(name, nameColor, bodyColor, playerId);
        if (GameManager.Instance.players.ContainsKey(playerId))
        {
            GameManager.Instance.players[playerId] = this;
        }
        else
        {
            GameManager.Instance.players.Add(playerId, this);
        }
    }

    [Rpc(SendTo.NotMe)]
    private void RequestPlayerDataRpc()
    {
        Instance.SendPlayerDataRpc(LobbyManager.Instance.playerName, LobbyManager.Instance.playerNameColor, LobbyManager.Instance.playerColor, AuthenticationService.Instance.PlayerId);
    }

    public void ResetPlayer()
    {
        networkGravity.ForceUnlock();

        if (IsServer)
            transform.SetParent(null);
        interactable.ForceRemoveInteract();
        if (!Rigidbody.isKinematic)
            Rigidbody.linearVelocity = Vector3.zero;

        SendPositionRpc(transform.position);

        EnablePhysics();
    }

    public void Die()
    {
        DieLocal();

        if (IsServer)
        {
            transform.SetParent(null);
        }

        SendDieRpc();
    }

    private void DieLocal()
    {
        if (!isDead)
        {
            ResetPlayer();
            isDead = true;
            OnStopMove?.Invoke();
            StartCoroutine(DeathAnimation());
            OnDeath?.Invoke();
        }
    }

    [Rpc(SendTo.NotMe)]
    private void SendDieRpc()
    {
        if (IsServer)
        {
            transform.SetParent(null); // Only server can set parent
        }

        DieLocal();
    }

    private void Respawn()
    {
        Rigidbody.isKinematic = false;
        isDead = false;
        playerModel.gameObject.SetActive(true);

        OnRespawn?.Invoke();
    }

    private IEnumerator DeathAnimation()
    {
        Rigidbody.isKinematic = true;
        playerModel.gameObject.SetActive(false);
        Vector3 gravityDirection = networkGravity.GetDirection();
        float networkGravityAcceleration = networkGravity.GetAcceleration();
        ParticleSystem dieParticles = Instantiate(dieEffectPrefab, transform.position, Quaternion.LookRotation(-gravityDirection)).GetComponent<ParticleSystem>();
        ParticleSystem.ForceOverLifetimeModule forceOverLifetime = dieParticles.forceOverLifetime;
        forceOverLifetime.x = gravityDirection.x * networkGravityAcceleration;
        forceOverLifetime.y = gravityDirection.y * networkGravityAcceleration;
        forceOverLifetime.z = gravityDirection.z * networkGravityAcceleration;
        dieParticles.Play();
        Destroy(dieParticles.gameObject, dieParticles.main.duration);
        yield return new WaitForSeconds(deathTime);
        if (canRespawn)
        {
            Respawn();
        }
    }

    [Rpc(SendTo.NotMe)]
    private void SendMoveRpc(Vector3 input)
    {
        moveInput = input;
    }

    [Rpc(SendTo.NotMe)]
    private void SendJumpRpc()
    {
        Rigidbody.linearVelocity -= networkGravity.GetDirection() * jumpVelocity;
    }

    [Rpc(SendTo.NotMe)]
    private void SendPositionRpc(Vector3 position)
    {
        transform.position = position;
    }

    public void EnablePhysics()
    {
        Rigidbody.isKinematic = false;
        playerCapsule.enabled = true;
    }

    public void DisablePhysics()
    {
        Rigidbody.isKinematic = true;
        playerCapsule.enabled = false;
    }
}
