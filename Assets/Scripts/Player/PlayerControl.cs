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
    public NetworkGravity gravity;
    private HealthSystem healthSystem;
    public Transform playerModel;
    private CapsuleCollider playerCapsule;
    public Interactable interactable;
    [SerializeField] private GameObject[] localOwnedObjects;
    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private MeshRenderer[] faceRenderers;
    [SerializeField] private TextMeshProUGUI nameText;

    public InputActions inputActions;
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

    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float obstacleDistance = 0.25f;
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

    [SerializeField] private bool cheatsEnabled = false;

    private void OnEnable()
    {
        inputActions = new InputActions();
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();
        healthSystem = GetComponent<HealthSystem>();
        playerCapsule = playerModel.GetComponent<CapsuleCollider>();

        if (IsOwner)
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                if (!IsServer)
                {
                    // Request that the host sends over everyones' data to this newly spawned player
                    UpdatePlayerServerRpc(LobbyManager.Instance.playerName, LobbyManager.Instance.playerNameColor, LobbyManager.Instance.playerColor, AuthenticationService.Instance.PlayerId);
                    NewPlayerUpdateServerRpc();
                }
                else
                {
                    UpdatePlayer(LobbyManager.Instance.playerName, LobbyManager.Instance.playerNameColor, LobbyManager.Instance.playerColor, AuthenticationService.Instance.PlayerId);
                }
                GameManager.Instance.players.Add(AuthenticationService.Instance.PlayerId, this);
            }
            else
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

    private void TogglePause()
    {
        if (IsPaused)
        {
            IsPaused = false;
            Cursor.visible = false;
        }
        else
        {
            OnStopMove?.Invoke();
            if (!IsServer)
                MoveServerRpc(Vector2.zero);
            IsPaused = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void GetHover()
    {
        if (isDead)
            return;

        // Raycast to get what the player is hovering over
        Vector3 screenPosition = inputActions.UI.Point.ReadValue<Vector2>();
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

        Vector3 newGravityDirection = Vector3.zero;
        Vector3 cameraForward = Camera.main.transform.forward;

        // Flattening camera forward vector to get a cardinal direction
        if (Mathf.Abs(cameraForward.x) > Mathf.Abs(cameraForward.y))
        {
            if (Mathf.Abs(cameraForward.x) > Mathf.Abs(cameraForward.z))
            {
                newGravityDirection.x = cameraForward.x < 0 ? -1f : 1f;
            }
            else
            {
                newGravityDirection.z = cameraForward.z < 0 ? -1f : 1f;
            }
        }
        else
        {
            if (Mathf.Abs(cameraForward.y) > Mathf.Abs(cameraForward.z))
            {
                newGravityDirection.y = cameraForward.y < 0 ? -1f : 1f;
            }
            else
            {
                newGravityDirection.z = cameraForward.z < 0 ? -1f : 1f;
            }
        }

        gravity.SetDirection(newGravityDirection);
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        if (IsOwner)
        {
            if (inputActions.UI.Menu.triggered)
            {
                TogglePause();
            }

            if (cheatsEnabled && inputActions.Player.LoadNextLevel.triggered)
            {
                LobbyManager.Instance.LoadNextLevel();
            }

            GetHover();
            if (inputActions.Player.Gravity.triggered)
            {
                SwitchGravity();
            }
        }

        // Rotate player to align with gravity
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, -gravity.GetDirection()) * transform.rotation;
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

        Vector3 gravityDirection = gravity.GetDirection();
        Vector3 moveDirection;

        if (!IsPaused)
        {
            if (IsOwner)
            {
                Vector2 newMoveInput = inputActions.Player.Move.ReadValue<Vector2>();
                /*moveDirection = transform.forward * newMoveInput.y + transform.right * newMoveInput.x;
                Vector3 offset = moveDirection * obstacleDistance;
                Vector3 start = playerModel.position - ((playerCapsule.height * 0.49f - playerCapsule.radius) * playerModel.up) + offset;
                Vector3 end = playerModel.position + ((playerCapsule.height * 0.49f - playerCapsule.radius) * playerModel.up) + offset;
                if (Physics.CheckCapsule(start, end, playerCapsule.radius, collisionLayers, QueryTriggerInteraction.Ignore))
                {
                    newMoveInput = Vector2.zero;
                    Debug.DrawLine(start, end, Color.red, 0.1f);
                }*/

                if (moveInput == Vector2.zero && newMoveInput != Vector2.zero)
                {
                    OnStartMove?.Invoke();
                }
                else if (newMoveInput == Vector2.zero && moveInput != Vector2.zero)
                {
                    OnStopMove?.Invoke();
                }

                if (moveInput != newMoveInput)
                {
                    if (IsServer)
                    {
                        MoveClientRpc(newMoveInput);
                        SyncPositionClientRpc(transform.position);
                    }
                    else
                    {
                        MoveServerRpc(newMoveInput);
                        SyncPositionServerRpc(transform.position);
                    }
                }
                moveInput = newMoveInput;

                if (isGrounded && inputActions.Player.Jump.ReadValue<float>() > 0)
                {
                    if (IsServer)
                    {
                        JumpClientRpc();
                        SyncPositionClientRpc(transform.position);
                    }
                    else
                    {
                        JumpServerRpc();
                        SyncPositionServerRpc(transform.position);
                    }
                    OnJump?.Invoke();
                    Rigidbody.linearVelocity -= gravityDirection * jumpVelocity;
                    isGrounded = false;
                    ground = null;
                    OnGroundChanged?.Invoke("Air");
                }
            }
        }
        else
        {
            moveInput = Vector2.zero;
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

        if (Vector3.Angle(collision.GetContact(0).normal, -gravity.GetDirection()) < 45f)
        {
            if (IsServer)
            {
                SyncPositionClientRpc(transform.position);
            }
            else
            {
                SyncPositionServerRpc(transform.position);
            }

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

        switch(other.tag)
        {
            case "DeathZone":
                Die();
                break;
        }
    }

    private void UpdatePlayer(string name, Color nameColor, Color bodyColor, string playerId) 
    {
        this.name = name;
        nameText.text = name;
        nameText.color = nameColor;
        bodyRenderer.material.color = bodyColor;
        foreach(MeshRenderer meshRenderer in faceRenderers)
        {
            meshRenderer.material.color = nameColor;
        }
        this.playerId = playerId;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerServerRpc(string name, Color nameColor, Color bodyColor, string playerId)
    {
        UpdatePlayer(name, nameColor, bodyColor, playerId);
        UpdatePlayerClientRpc(name, nameColor, bodyColor, playerId);
        if (GameManager.Instance.players.ContainsKey(playerId))
        {
            GameManager.Instance.players[playerId] = this;
        }
        else 
        {
            GameManager.Instance.players.Add(playerId, this);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void UpdatePlayerClientRpc(string name, Color nameColor, Color bodyColor, string playerId)
    {
        UpdatePlayer(name, nameColor, bodyColor, playerId);
        if (GameManager.Instance.players.ContainsKey(playerId))
        {
            GameManager.Instance.players[playerId] = this;
        }
        else
        {
            GameManager.Instance.players.Add(playerId, this);
        }
    }

    [ServerRpc]
    private void NewPlayerUpdateServerRpc()
    {
        Instance.UpdatePlayerClientRpc(LobbyManager.Instance.playerName, LobbyManager.Instance.playerNameColor, LobbyManager.Instance.playerColor, AuthenticationService.Instance.PlayerId);
        NewPlayerUpdateClientRpc();
    }

    [ClientRpc]
    private void NewPlayerUpdateClientRpc()
    {
        Instance.UpdatePlayerServerRpc(LobbyManager.Instance.playerName, LobbyManager.Instance.playerNameColor, LobbyManager.Instance.playerColor, AuthenticationService.Instance.PlayerId);
    }

    public void ResetPlayer()
    {
        if (IsServer)
            transform.SetParent(null);
        interactable.ForceRemoveInteract();
        healthSystem.ResetHealth();
        Rigidbody.linearVelocity = Vector3.zero;
        if (IsServer)
        {
            SyncPositionClientRpc(transform.position);
        }
        else
        {
            SyncPositionServerRpc(transform.position);
        }
        gravity.ForceUnlock();
    }

    public void Die()
    {
        if (IsServer)
        {
            transform.SetParent(null);
            DieLocal();
            DieClientRpc();
        }
        else
        {
            DieServerRpc();
        }
    }

    private void DieLocal()
    {
        ResetPlayer();
        isDead = true;
        OnStopMove?.Invoke();
        StartCoroutine(DeathAnimation());
        OnDeath?.Invoke();
        healthSystem.ResetHealth();
    }

    [ServerRpc]
    private void DieServerRpc()
    {
        transform.SetParent(null);
        DieLocal();
        DieClientRpc();
    }

    [ClientRpc]
    private void DieClientRpc()
    {
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
        Vector3 gravityDirection = gravity.GetDirection();
        float gravityAcceleration = gravity.GetAcceleration();
        ParticleSystem dieParticles = Instantiate(dieEffectPrefab, transform.position, Quaternion.LookRotation(-gravityDirection)).GetComponent<ParticleSystem>();
        ParticleSystem.ForceOverLifetimeModule forceOverLifetime = dieParticles.forceOverLifetime;
        forceOverLifetime.x = gravityDirection.x * gravityAcceleration;
        forceOverLifetime.y = gravityDirection.y * gravityAcceleration;
        forceOverLifetime.z = gravityDirection.z * gravityAcceleration;
        dieParticles.Play();
        Destroy(dieParticles.gameObject, dieParticles.main.duration);
        yield return new WaitForSeconds(deathTime);
        if (canRespawn)
        {
            Respawn();
        }
    }

    [ServerRpc]
    private void MoveServerRpc(Vector3 input)
    {
        moveInput = input;
        MoveClientRpc(input);
    }

    [ClientRpc(RequireOwnership = false)]
    private void MoveClientRpc(Vector3 input)
    {
        if (!IsOwner)
            moveInput = input;
    }

    [ServerRpc]
    private void JumpServerRpc()
    {
        Rigidbody.linearVelocity -= gravity.GetDirection() * jumpVelocity;
        JumpClientRpc();
    }

    [ClientRpc(RequireOwnership = false)]
    private void JumpClientRpc()
    {
        if (!IsOwner && !IsServer)
            Rigidbody.linearVelocity -= gravity.GetDirection() * jumpVelocity;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SyncPositionServerRpc(Vector3 position)
    {
        transform.position = position;
        SyncPositionClientRpc(position);
    }

    [ClientRpc(RequireOwnership = false)]
    private void SyncPositionClientRpc(Vector3 position)
    {
        if (!IsOwner)
        {
            transform.position = position;
        }
    }
}
