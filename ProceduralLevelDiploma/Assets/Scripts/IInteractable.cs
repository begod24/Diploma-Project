using UnityEngine;

public interface IInteractable
{
    void Interact();
    void OnHighlightStart();
    void OnHighlightEnd();
    string GetInteractionText();
    bool CanInteract();
}
