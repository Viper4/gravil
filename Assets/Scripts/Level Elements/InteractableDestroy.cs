using UnityEngine;

public class InteractableDestroy : MonoBehaviour
{
    [SerializeField, ColorUsage(true, true)] private Color dissolveColor;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger)
        {
            if (other.TryGetComponent<Interactable>(out var interactable) && interactable.destructible && !interactable.isDissolving)
            {
                interactable.Dissolve(dissolveColor);
            }
        }
    }
}
