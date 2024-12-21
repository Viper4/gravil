using UnityEngine;
using Unity;

[RequireComponent(typeof(Rigidbody))]
public class GrabbableRigidbody : MonoBehaviour
{
    private Rigidbody rb;
    private Collider _collider;

    private bool ownerInTrigger;
    private bool isGrabbed;

    [SerializeField] private float grabDistance = 0.5f;

    [SerializeField] private GameObject highlight;
    [SerializeField] private GameObject popup;

    [SerializeField] private LayerMask solidLayers;

    private float extents;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        extents = _collider.bounds.extents.z;
    }

    private void Update()
    {
        if (ownerInTrigger)
        {
            if (isGrabbed)
            {
                highlight.SetActive(false);
                popup.SetActive(false);
                if (PlayerControl.Instance.inputActions.Player.Interact.triggered)
                {
                    isGrabbed = false;
                    _collider.enabled = true;
                    rb.isKinematic = false;
                }
            }
            else
            {
                if (PlayerControl.Instance.hoveredObject == transform)
                {
                    highlight.SetActive(true);
                    popup.SetActive(true);
                    if (PlayerControl.Instance.inputActions.Player.Interact.triggered)
                    {
                        highlight.SetActive(false);
                        isGrabbed = true;
                        _collider.enabled = false;
                        rb.isKinematic = true;
                    }
                }
                else
                {
                    highlight.SetActive(false);
                    popup.SetActive(false);
                }
            }
        }

        if (isGrabbed)
        {
            if (Physics.Raycast(PlayerControl.Instance.playerModel.position, PlayerControl.Instance.playerModel.forward, out RaycastHit hit, grabDistance + extents, solidLayers))
            {
                transform.position = hit.point + (hit.normal * extents);
            }
            else
            {
                transform.position = PlayerControl.Instance.playerModel.position + PlayerControl.Instance.playerModel.forward * grabDistance;
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
                highlight.SetActive(false);
                popup.SetActive(false);
                isGrabbed = false;
            }
        }
    }
}
