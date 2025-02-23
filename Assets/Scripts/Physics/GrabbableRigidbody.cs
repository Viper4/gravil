using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class GrabbableRigidbody : NetworkBehaviour
{
    private NetworkRigidbodyTransfer networkRigidbodyTransfer;
    private Rigidbody attachedRigidbody;
    private Collider _collider;
    private Interactable interactable;

    private bool ownerInTrigger;
    private bool isGrabbed = false;

    [SerializeField] private float grabDistance = 0.5f;

    public Popup popup;

    [SerializeField] private LayerMask solidLayers;

    private float extents;

    [SerializeField] private bool setColliderEnabled = true;
    private int originalLayer;
    [SerializeField] private int grabbedLayer = 2;

    private string grabberId;

    private NetworkTransform networkTransform;

    private Vector3 previousPosition;

    private void Start()
    {
        networkRigidbodyTransfer = GetComponent<NetworkRigidbodyTransfer>();
        attachedRigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        interactable = GetComponent<Interactable>();
        networkTransform = GetComponent<NetworkTransform>();
        extents = _collider.bounds.extents.z;
        originalLayer = gameObject.layer;
    }

    private void FixedUpdate()
    {
        if (isGrabbed)
            previousPosition = transform.position;
    }

    private void Update()
    {
        if (isGrabbed)
        {
            attachedRigidbody.isKinematic = true;
            if (gameObject.layer != grabbedLayer)
            {
                if (setColliderEnabled)
                    _collider.enabled = false;
                popup.Hide();
                gameObject.layer = grabbedLayer;

                networkTransform.enabled = false;
            }

            if (grabberId.Length > 0)
            {
                if (GameManager.Instance.players.ContainsKey(grabberId) && GameManager.Instance.players[grabberId] != null)
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

                    if (PlayerControl.Instance.playerId == grabberId)
                    {
                        if (GameManager.Instance.inputActions.Player.Interact.triggered || PlayerControl.Instance.isDead)
                        {
                            Release();
                        }
                    }
                }
                else
                {
                    if (IsServer)
                    {
                        Release();
                    }
                    else
                    {
                        RequestGrabberIdRpc();
                    }
                }
            }
        }
        else
        {
            if (gameObject.layer != originalLayer)
            {
                if (setColliderEnabled)
                    _collider.enabled = true;
                gameObject.layer = originalLayer;

                networkTransform.enabled = true;
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
        if (!isGrabbed && !other.isTrigger && other.CompareTag("Player"))
        {
            if (other.attachedRigidbody.GetComponent<PlayerControl>().IsOwner)
            {
                ownerInTrigger = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isGrabbed && !other.isTrigger && other.CompareTag("Player"))
        {
            if (other.attachedRigidbody.GetComponent<PlayerControl>().IsOwner)
            {
                ownerInTrigger = false;
                popup.Hide();
            }
        }
    }

    [Rpc(SendTo.NotMe)]
    private void SetGrabRpc(bool isGrabbed, string playerId)
    {
        this.isGrabbed = isGrabbed;
        grabberId = playerId;
    }

    public void Grab(string playerId)
    {
        if (grabberId == PlayerControl.Instance.playerId)
            PlayerControl.Instance.OnRespawn += Release;
        if (interactable != null)
        {
            interactable.ForceRemoveInteract();
        }
        if (networkRigidbodyTransfer != null)
        {
            networkRigidbodyTransfer.ChangeOwnership(GameManager.Instance.players[playerId].OwnerClientId);
        }

        isGrabbed = true;
        grabberId = playerId;

        SetGrabRpc(true, playerId);
    }

    [Rpc(SendTo.NotMe)]
    private void SendReleaseRpc()
    {
        isGrabbed = false;
        grabberId = string.Empty;
        attachedRigidbody.isKinematic = false;
        attachedRigidbody.linearVelocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
    }

    public void Release()
    {
        if (grabberId == PlayerControl.Instance.playerId)
            PlayerControl.Instance.OnRespawn -= Release;

        isGrabbed = false;
        grabberId = string.Empty;
        attachedRigidbody.isKinematic = false;
        attachedRigidbody.linearVelocity = (transform.position - previousPosition) / Time.fixedDeltaTime;

        SendReleaseRpc();
    }

    [Rpc(SendTo.Server)]
    public void SetParentNullRpc()
    {
        transform.SetParent(null);
    }

    [Rpc(SendTo.Server)]
    private void RequestGrabberIdRpc()
    {
        if (isGrabbed)
        {
            if (GameManager.Instance.players.ContainsKey(grabberId))
            {
                SetGrabRpc(true, grabberId);
            }
            else
            {
                Release();
            }
        }
        else
        {
            Release();
        }
    }
}
