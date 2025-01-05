using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Laser : MonoBehaviour
{
    [SerializeField] private bool emitting = false;
    [SerializeField] private Transform laserPivot;
    [SerializeField] private MeshRenderer laserRenderer;
    [SerializeField, ColorUsage(false, true)] private Color laserColor;

    private Vector3 startScale;
    private float laserLength = 100f;

    [SerializeField] private LayerMask ignoreLayers;
    [SerializeField] private bool destroyInteractables = false;
    [SerializeField, ColorUsage(false, true)] private Color dissolveColor;

    private void Start()
    {
        laserRenderer.material.SetColor("_EmissionColor", laserColor);
        startScale = laserPivot.localScale;
        if (emitting)
        {
            Activate();
        }
        else
        {
            Deactivate();
        }
    }

    private void FixedUpdate()
    {
        if (emitting)
        {
            float radius = Mathf.Max(laserPivot.localScale.x, laserPivot.localScale.z) * Mathf.Max(laserRenderer.transform.localScale.x, laserRenderer.transform.localScale.z) * 0.5f;
            if (Physics.SphereCast(laserPivot.position, radius, laserPivot.up, out RaycastHit hit, 100f, ~ignoreLayers, QueryTriggerInteraction.Ignore))
            {
                laserLength = hit.distance + radius;
                if (destroyInteractables)
                {
                    if (hit.transform.TryGetComponent<Interactable>(out var interactable) && interactable.destructible && !interactable.isDissolving)
                    {
                        interactable.Dissolve(dissolveColor);
                    }
                }
            }
            else
            {
                laserLength = 100f;
            }
            laserPivot.localScale = new Vector3(startScale.x, laserLength, startScale.z);
        }
    }

    public void Activate()
    {
        emitting = true;
        laserPivot.localScale = new Vector3(startScale.x, laserLength, startScale.z);
    }

    public void Deactivate()
    {
        emitting = false;
        laserPivot.localScale = new Vector3(startScale.x, 0f, startScale.z);
    }
}
