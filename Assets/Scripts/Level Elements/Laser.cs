using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Laser : MonoBehaviour
{
    [SerializeField] private bool emitting = false;
    [SerializeField] private Transform laserPivot;
    [SerializeField] private MeshRenderer laserRenderer;

    [SerializeField] private Vector3 minScale;
    [SerializeField] private Vector3 maxScale;
    private float laserLength = 100f;

    [SerializeField, ColorUsage(false, true)] private Color emittingColor;
    [SerializeField, ColorUsage(false, true)] private Color inactiveColor;

    [SerializeField] private LayerMask ignoreLayers;

    private void FixedUpdate()
    {
        if (emitting)
        {
            if (Physics.Raycast(laserPivot.position, laserPivot.up, out RaycastHit hit, 100f, ~ignoreLayers, QueryTriggerInteraction.Ignore))
            {
                laserLength = hit.distance;
            }
            else
            {
                laserLength = 100f;
            }
            laserPivot.localScale = new Vector3(laserPivot.localScale.x, laserLength, laserPivot.localScale.z);
        }
    }

    public void Activate(float t)
    {
        emitting = true;
        laserRenderer.material.SetColor("_EmissionColor", emittingColor);
        laserPivot.localScale = Vector3.Lerp(minScale, maxScale, t);
        laserPivot.localScale = new Vector3(laserPivot.localScale.x, laserLength, laserPivot.localScale.z);
    }

    public void Deactivate(float t)
    {
        laserRenderer.material.SetColor("_EmissionColor", Color.Lerp(emittingColor, inactiveColor, t));
        laserPivot.localScale = Vector3.Lerp(maxScale, minScale, t);
        if (t < 1)
        {
            laserPivot.localScale = new Vector3(laserPivot.localScale.x, laserLength, laserPivot.localScale.z);
        }
        else
        {
            emitting = false;
        }
    }
}
