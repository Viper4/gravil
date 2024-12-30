using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkRigidbodyTransfer))]
public class GrabbableRigidbody : NetworkBehaviour
{
    private NetworkRigidbodyTransfer networkRigidbodyTransfer;
    private Collider _collider;
    private Interactable buttonInteractable;

    private bool ownerInTrigger;
    [SerializeField] private NetworkVariable<bool> isGrabbed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private float grabDistance = 0.5f;

    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject popup;

    [SerializeField] private LayerMask solidLayers;

    private float extents;

    [SerializeField] private bool setColliderEnabled = true;
    private int originalLayer;
    [SerializeField] private int grabbedLayer = 2;

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
        _collider = GetComponent<Collider>();
        buttonInteractable = GetComponent<Interactable>();
        extents = _collider.bounds.extents.z;
        originalLayer = gameObject.layer;
    }

    private void Update()
    {
        if (isGrabbed.Value)
        {
            if (setColliderEnabled)
                _collider.enabled = false;
            ToggleUI(false);
            networkRigidbodyTransfer.Rigidbody.isKinematic = true;
            gameObject.layer = grabbedLayer;

            if (IsOwner)
            {
                if (Physics.Raycast(PlayerControl.Instance.playerModel.position, PlayerControl.Instance.playerModel.forward, out RaycastHit hit, grabDistance + extents, solidLayers))
                {
                    transform.position = hit.point + (hit.normal * extents);
                }
                else
                {
                    transform.position = PlayerControl.Instance.playerModel.position + PlayerControl.Instance.playerModel.forward * grabDistance;
                }

                if (PlayerControl.Instance.inputActions.Player.Interact.triggered || PlayerControl.Instance.isDead)
                {
                    Release();
                }
            }
        }
        else
        {
            if (setColliderEnabled)
                _collider.enabled = true;
            gameObject.layer = originalLayer;

            if (ownerInTrigger && PlayerControl.Instance.hoveredObject == transform && !PlayerControl.Instance.isDead)
            {
                ToggleUI(true);
                if (PlayerControl.Instance.inputActions.Player.Interact.triggered)
                {
                    Grab(PlayerControl.Instance.OwnerClientId);
                }
            }
            else
            {
                ToggleUI(false);
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
                ToggleUI(false);
            }
        }
    }

    public void Grab(ulong clientId)
    {
        if (buttonInteractable != null)
        {
            buttonInteractable.ForceRemoveInteract();
        }
        if (IsServer)
        {
            if (networkRigidbodyTransfer != null)
            {
                networkRigidbodyTransfer.ChangeOwnership(clientId);
                networkRigidbodyTransfer.canChangeOwnership = false;
            }
            isGrabbed.Value = true;
        }
        else
        {
            GrabServerRpc(clientId);
        }
    }

    public void Release()
    {
        if (IsServer)
        {
            if (networkRigidbodyTransfer != null)
            {
                networkRigidbodyTransfer.canChangeOwnership = true;
            }
            isGrabbed.Value = false;
        }
        else
        {
            ReleaseServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void GrabServerRpc(ulong clientId)
    {
        if (buttonInteractable != null)
        {
            buttonInteractable.ForceRemoveInteract();
        }
        if (networkRigidbodyTransfer != null)
        {
            networkRigidbodyTransfer.ChangeOwnership(clientId);
            networkRigidbodyTransfer.canChangeOwnership = false;
        }
        isGrabbed.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReleaseServerRpc()
    {
        if (networkRigidbodyTransfer != null)
        {
            networkRigidbodyTransfer.canChangeOwnership = true;
        }
        isGrabbed.Value = false;
    }

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        if(isGrabbed.Value)
        {
            ToggleUI(false);
            networkRigidbodyTransfer.Rigidbody.isKinematic = true;
        }
        else
        {
            networkRigidbodyTransfer.Rigidbody.isKinematic = false;
        }
    }

    private void OnIsGrabbedChanged(bool previousValue, bool newValue)
    {
        if (IsOwner && previousValue && !newValue)
        {
            networkRigidbodyTransfer.Rigidbody.isKinematic = false;
            networkRigidbodyTransfer.Rigidbody.linearVelocity = PlayerControl.Instance.gravity.attachedRigidbody.linearVelocity;
        }
    }

    public void ToggleUI(bool value)
    {
        highlight.SetActive(value);
        popup.SetActive(value);
    }
}
