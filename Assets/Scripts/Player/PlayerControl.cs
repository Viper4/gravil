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

    public InputActions inputActions;
    public string playerId;
    public bool paused = true;

    private Vector2 moveInput;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpVelocity = 2f;

    private Transform ground;
    [HideInInspector] public bool isGrounded = false;

    [SerializeField] private float maxLeanAngle = 15f;
    [SerializeField] private float leanSpeed = 10f;

    public Transform playerModel;
    public Interactable buttonInteractable;

    [SerializeField] private GameObject[] localOwnedObjects;

    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private MeshRenderer[] faceRenderers;

    public NetworkGravity gravity;
    [SerializeField] private float gravityRotateSpeed = 125f;

    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private LayerMask hoverLayers;

    [SerializeField] private TextMeshProUGUI nameText;

    [HideInInspector] public Transform hoveredObject;

    private Rigidbody attachedRigidbody;

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
        attachedRigidbody = GetComponent<Rigidbody>();

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

    private void Update()
    {
        if (!IsOwner || !IsSpawned)
            return;

        if (inputActions.UI.Menu.triggered)
        {
            if (paused)
            {
                paused = false;
                Cursor.visible = false;
            }
            else
            {
                OnStopMove?.Invoke();
                paused = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // Raycast to get what the player is hovering over
        Vector3 screenPosition = inputActions.UI.Point.ReadValue<Vector2>();
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hoverHit, Mathf.Infinity, hoverLayers))
        {
            Debug.DrawLine(ray.origin, hoverHit.point, Color.red, 0.1f);
            hoveredObject = hoverHit.transform;
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction, Color.green, 0.1f);
            hoveredObject = null;
        }

        if (isDead)
            return;

        if (inputActions.Player.Gravity.triggered)
        {
            Vector3 newGravityDirection = Vector3.zero;
            Vector3 cameraForward = Camera.main.transform.forward;
            
            // Flattening camera forward vector to get a cardinal direction
            if (Mathf.Abs(cameraForward.x) > Mathf.Abs(cameraForward.y))
            {
                if(Mathf.Abs(cameraForward.x) > Mathf.Abs(cameraForward.z))
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
                if(Mathf.Abs(cameraForward.y) > Mathf.Abs(cameraForward.z))
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

        // Rotate player to align with gravity
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, -gravity.direction) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, gravityRotateSpeed * Time.deltaTime);

        // Rotate player model to lean with movement
        playerModel.localEulerAngles = new Vector3(playerModel.localEulerAngles.x, Camera.main.transform.localEulerAngles.y, playerModel.localEulerAngles.z);
        Vector3 lean = new Vector3(moveInput.y * maxLeanAngle, Camera.main.transform.localEulerAngles.y, -moveInput.x * maxLeanAngle);
        playerModel.localRotation = Quaternion.RotateTowards(playerModel.localRotation, Quaternion.Euler(lean), leanSpeed * Time.deltaTime);

        if (cheatsEnabled)
        {
            if (inputActions.Player.LoadNextLevel.triggered)
            {
                LobbyManager.Instance.LoadNextLevel();
            }
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !IsSpawned || isDead)
            return;

        if (!paused)
        {
            Vector2 newMoveInput = inputActions.Player.Move.ReadValue<Vector2>();
            if(moveInput == Vector2.zero && newMoveInput != Vector2.zero)
            {
                OnStartMove?.Invoke();
            }
            else if(newMoveInput == Vector2.zero && moveInput != Vector2.zero)
            {
                OnStopMove?.Invoke();
            }
            moveInput = newMoveInput;

            Vector3 moveDirection = playerModel.forward * moveInput.y + playerModel.right * moveInput.x;
            Vector3 verticalVelocity = Vector3.Project(attachedRigidbody.linearVelocity, gravity.direction);
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(moveDirection, gravity.direction).normalized * moveSpeed;
            Vector3 goalVelocity = verticalVelocity + horizontalVelocity;
            Vector3 acceleration = (goalVelocity - attachedRigidbody.linearVelocity) / Time.fixedDeltaTime;
            attachedRigidbody.AddForce(acceleration, ForceMode.Acceleration);

            if (isGrounded && inputActions.Player.Jump.ReadValue<float>() > 0)
            {
                OnJump?.Invoke();
                attachedRigidbody.linearVelocity -= gravity.direction * jumpVelocity;
                isGrounded = false;
                ground = null;
                OnGroundChanged?.Invoke("Air");
            }
        }
        else
        {
            moveInput = Vector2.zero;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner)
            return;

        if (Vector3.Angle(collision.GetContact(0).normal, -gravity.direction) < 45f)
        {
            ground = collision.transform;
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

        if (collision.transform == ground)
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
                if (IsServer)
                {
                    Die();
                    DieClientRpc();
                }
                else
                {
                    DieServerRpc();
                }
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
            meshRenderer.material.color = new Color(1 - nameColor.r, 1 - nameColor.g, 1 - nameColor.b, nameColor.a);
        }
        this.playerId = playerId;
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerServerRpc(string name, Color nameColor, Color bodyColor, string playerId)
    {
        UpdatePlayer(name, nameColor, bodyColor, playerId);
        UpdatePlayerClientRpc(name, nameColor, bodyColor, playerId);
    }

    [ClientRpc(RequireOwnership = false)]
    public void UpdatePlayerClientRpc(string name, Color nameColor, Color bodyColor, string playerId)
    {
        UpdatePlayer(name, nameColor, bodyColor, playerId);
    }

    [ServerRpc]
    private void NewPlayerUpdateServerRpc()
    {
        if (IsServer)
        {
            Instance.UpdatePlayerClientRpc(LobbyManager.Instance.playerName, LobbyManager.Instance.playerNameColor, LobbyManager.Instance.playerColor, AuthenticationService.Instance.PlayerId);
        }
    }

    private void Die()
    {
        isDead = true;
        OnStopMove?.Invoke();
        StartCoroutine(DeathAnimation());
        OnDeath?.Invoke();
    }

    [ServerRpc]
    private void DieServerRpc()
    {
        Die();
        DieClientRpc();
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        Die();
    }

    private void Respawn()
    {
        attachedRigidbody.isKinematic = false;
        isDead = false;
        playerModel.gameObject.SetActive(true);
        OnRespawn?.Invoke();
    }

    private IEnumerator DeathAnimation()
    {
        attachedRigidbody.isKinematic = true;
        playerModel.gameObject.SetActive(false);
        ParticleSystem dieParticles = Instantiate(dieEffectPrefab, transform.position, Quaternion.LookRotation(-gravity.direction)).GetComponent<ParticleSystem>();
        ParticleSystem.ForceOverLifetimeModule forceOverLifetime = dieParticles.forceOverLifetime;
        forceOverLifetime.x = gravity.direction.x * gravity.acceleration;
        forceOverLifetime.y = gravity.direction.y * gravity.acceleration;
        forceOverLifetime.z = gravity.direction.z * gravity.acceleration;
        dieParticles.Play();
        Destroy(dieParticles.gameObject, dieParticles.main.duration);
        yield return new WaitForSeconds(deathTime);
        if (canRespawn)
        {
            Respawn();
        }
    }
}
