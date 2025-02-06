using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GrabbableRigidbody : NetworkBehaviour
{
    private NetworkRigidbodyTransfer networkRigidbodyTransfer;
    private Rigidbody attachedRigidbody;
    private Collider _collider;
    private Interactable interactable;

    private bool ownerInTrigger;
    [SerializeField] private NetworkVariable<bool> isGrabbed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private float grabDistance = 0.5f;

    public Popup popup;

    [SerializeField] private LayerMask solidLayers;

    private float extents;

    [SerializeField] private bool setColliderEnabled = true;
    private int originalLayer;
    [SerializeField] private int grabbedLayer = 2;

    private NetworkVariable<FixedString64Bytes> grabbedPlayerId = new NetworkVariable<FixedString64Bytes>(string.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void OnEnable()
    {
        isGrabbed.OnValueChanged += OnIsGrabbedChanged;
    }

    private void OnDisable()
    {
        isGrabbed.OnValueChanged -= OnIsGrabbedChanged;
    }

    private void Start()
    {
        networkRigidbodyTransfer = GetComponent<NetworkRigidbodyTransfer>();
        attachedRigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        interactable = GetComponent<Interactable>();
        extents = _collider.bounds.extents.z;
        originalLayer = gameObject.layer;
    }

    private void Update()
    {
        if (isGrabbed.Value)
        {
            if (gameObject.layer != grabbedLayer)
            {
                if (setColliderEnabled)
                    _collider.enabled = false;
                popup.Hide();
                attachedRigidbody.isKinematic = true;
                gameObject.layer = grabbedLayer;
            }
            
            if (grabbedPlayerId.Value.Length > 0)
            {
                string grabberId = grabbedPlayerId.Value.ToString();
                if (IsOwner)
                {
                    PlayerControl player = GameManager.Instance.players[grabberId];
                    if (Physics.Raycast(player.playerModel.position, player.playerModel.forward, out RaycastHit hit, grabDistance + extents, solidLayers))
                    {
                        transform.position = hit.point + (hit.normal * extents);
                    }
                    else
                    {
                        transform.position = player.playerModel.position + player.playerModel.forward * grabDistance;
                    }
                }
                
                if (PlayerControl.Instance.playerId == grabberId)
                {
                    if (GameManager.Instance.inputActions.Player.Interact.triggered || PlayerControl.Instance.isDead)
                    {
                        Release();
                    }
                }
            }
        }
        else
        {
            if(gameObject.layer != originalLayer)
            {
                if (setColliderEnabled)
                    _collider.enabled = true;
                gameObject.layer = originalLayer;
            }

            if (ownerInTrigger && PlayerControl.Instance.HoveredObject == transform && !PlayerControl.Instance.isDead)
            {
                popup.Show("'E' to grab");
                if (GameManager.Instance.inputActions.Player.Interact.triggered)
                {
                    Grab(PlayerControl.Instance.playerId);
                }
            }
            else
            {
                popup.Hide();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isGrabbed.Value && !other.isTrigger && other.CompareTag("Player"))
        {
            if (other.attachedRigidbody.GetComponent<PlayerControl>().IsOwner)
            {
                ownerInTrigger = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isGrabbed.Value && !other.isTrigger && other.CompareTag("Player"))
        {
            if (other.attachedRigidbody.GetComponent<PlayerControl>().IsOwner)
            {
                ownerInTrigger = false;
                popup.Hide();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetGrabServerRpc(bool isGrabbed, string playerId)
    {
        this.isGrabbed.Value = isGrabbed;
        grabbedPlayerId.Value = playerId;
    }

    public void Grab(string playerId)
    {
        PlayerControl.Instance.OnRespawn += Release;
        if (interactable != null)
        {
            interactable.ForceRemoveInteract();
        }
        if (networkRigidbodyTransfer != null)
        {
            networkRigidbodyTransfer.ChangeOwnership(GameManager.Instance.players[playerId].OwnerClientId);
        }

        if (IsServer)
        {
            isGrabbed.Value = true;
            grabbedPlayerId.Value = playerId;
        }
        else
        {
            SetGrabServerRpc(true, playerId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReleaseServerRpc()
    {
        isGrabbed.Value = false;
        grabbedPlayerId.Value = string.Empty;
    }

    public void Release()
    {
        PlayerControl.Instance.OnRespawn -= Release;
        if (IsServer)
        {
            isGrabbed.Value = false;
            grabbedPlayerId.Value = string.Empty;
        }
        else
        {
            ReleaseServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetParentNullServerRpc()
    {
        transform.SetParent(null);
    }

    private void OnIsGrabbedChanged(bool previousValue, bool newValue)
    {
        if (IsOwner && previousValue && !newValue)
        {
            attachedRigidbody.isKinematic = false;
            attachedRigidbody.linearVelocity = PlayerControl.Instance.Rigidbody.linearVelocity;
        }
    }
}
