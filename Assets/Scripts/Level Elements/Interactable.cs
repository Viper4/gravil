using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class Interactable : MonoBehaviour
{
    private Rigidbody attachedRigidbody;
    private Collider[] colliders;
    private MeshRenderer meshRenderer;
    private GrabbableRigidbody grabbableRigidbody;

    public bool canInteract = true;
    public bool destructible = false;
    [SerializeField] private bool canRespawn = false;
    [SerializeField] private GameObject dissolveEffectPrefab;
    [SerializeField] private float respawnDelay = 1.0f;
    [SerializeField] private float dissolveScale = 25f;

    [HideInInspector] public LevelButton interactedButton;

    public bool isDissolving { get; private set; }

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Start()
    {
        attachedRigidbody = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();
        grabbableRigidbody = GetComponent<GrabbableRigidbody>();
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void ForceRemoveInteract()
    {
        if(interactedButton != null)
        {
            interactedButton.RemoveObject(this);
        }
    }

    private void ToggleActive(bool value)
    {
        canInteract = value;

        if (attachedRigidbody != null)
            attachedRigidbody.isKinematic = !value;
        foreach (Collider collider in colliders)
        {
            collider.enabled = value;
        }
        meshRenderer.enabled = value;

        if (grabbableRigidbody != null)
        {
            grabbableRigidbody.ToggleUI(value);
            grabbableRigidbody.enabled = value;
        }
    }

    public void Respawn()
    {
        isDissolving = false;
        ToggleActive(true);

        if (attachedRigidbody != null && !attachedRigidbody.isKinematic)
        {
            attachedRigidbody.linearVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(startPosition, startRotation);
    }

    public void Dissolve(Color dissolveColor)
    {
        if (!destructible)
            return;

        isDissolving = true;
        ForceRemoveInteract();

        if (dissolveEffectPrefab != null)
        {
            GameObject newEffect = Instantiate(dissolveEffectPrefab, transform.position, transform.rotation);
            newEffect.transform.localScale = transform.localScale;
            VisualEffect visualEffect = newEffect.GetComponent<VisualEffect>();
            visualEffect.SetFloat("Lifetime", respawnDelay);
            visualEffect.SetMesh("Mesh", meshRenderer.GetComponent<MeshFilter>().mesh);
            if (meshRenderer.material.mainTexture != null)
                visualEffect.SetTexture("MainTex", meshRenderer.material.mainTexture);
            visualEffect.SetVector4("MainColor", meshRenderer.material.color);
            visualEffect.SetVector4("DissolveColor", dissolveColor);
            visualEffect.SetFloat("Scale", dissolveScale);
            visualEffect.Play();

            Rigidbody effectRigidbody = newEffect.GetComponent<Rigidbody>();
            if (effectRigidbody != null)
            {
                effectRigidbody.linearVelocity = attachedRigidbody.linearVelocity / 10f;
                effectRigidbody.angularVelocity = attachedRigidbody.angularVelocity / 10f;
            }

            Destroy(newEffect, respawnDelay);
        }

        if (canRespawn)
        {
            ToggleActive(false);

            Invoke(nameof(Respawn), respawnDelay);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
