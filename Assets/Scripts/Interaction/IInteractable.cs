using UnityEngine;

public interface IInteractable
{
    bool CanInteract(InteractionContext context, out string failureReason);
    string GetPrompt(InteractionContext context);
    Vector3 GetInteractionPoint();
    void Interact(InteractionContext context);
}
