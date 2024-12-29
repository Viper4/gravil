using UnityEngine;

public class ButtonInteractable : MonoBehaviour
{
    public bool canInteract = true;

    public LevelButton interactedButton;

    public void ForceRemoveInteract()
    {
        if(interactedButton != null)
        {
            interactedButton.RemoveObject(this);
        }
    }
}
