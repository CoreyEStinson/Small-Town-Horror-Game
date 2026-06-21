using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public sealed class InteractableObject : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [SerializeField] private string actionText = "Interact";
    [SerializeField] private string blockedActionText = "";
    [SerializeField] private bool showBlockedReason = true;
    [SerializeField] private string fallbackBlockedReason = "";

    [Header("Interaction")]
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private bool canRepeat = true;
    [SerializeField] private bool disableAfterUse;

    [Header("Rules")]
    [SerializeField] private List<InteractionCondition> conditions = new List<InteractionCondition>();

    [Header("Effects")]
    [SerializeField] private List<InteractionEffect> effects = new List<InteractionEffect>();

    [Header("Hooks")]
    [SerializeField] private UnityEvent onInteractSucceeded;
    [SerializeField] private UnityEvent onInteractFailed;

    private bool hasBeenUsed;

    public bool CanInteract(InteractionContext context, out string failureReason)
    {
        if (!canRepeat && hasBeenUsed)
        {
            failureReason = ResolveBlockedReason();
            return false;
        }

        return MeetsConditions(context, out failureReason);
    }

    public string GetPrompt(InteractionContext context)
    {
        if (!CanInteract(context, out _) && !string.IsNullOrWhiteSpace(blockedActionText))
        {
            return blockedActionText;
        }

        return actionText;
    }

    public Vector3 GetInteractionPoint()
    {
        return interactionPoint != null ? interactionPoint.position : transform.position;
    }

    public void Interact(InteractionContext context)
    {
        if (!CanInteract(context, out _))
        {
            onInteractFailed?.Invoke();
            return;
        }

        if (context != null)
        {
            context.sourceObject = gameObject;
            context.sourceTransform = transform;
        }

        RunEffects(context);
        onInteractSucceeded?.Invoke();

        if (!canRepeat)
        {
            hasBeenUsed = true;
        }

        if (disableAfterUse)
        {
            gameObject.SetActive(false);
        }
    }

    private bool MeetsConditions(InteractionContext context, out string failureReason)
    {
        for (int i = 0; i < conditions.Count; i++)
        {
            InteractionCondition condition = conditions[i];
            if (condition == null)
            {
                continue;
            }

            if (!condition.Evaluate(context))
            {
                failureReason = showBlockedReason
                    ? condition.GetFailureReason(context)
                    : string.Empty;
                return false;
            }
        }

        failureReason = string.Empty;
        return true;
    }

    private void RunEffects(InteractionContext context)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            InteractionEffect effect = effects[i];
            if (effect == null)
            {
                continue;
            }

            effect.Execute(context);
        }
    }

    private string ResolveBlockedReason()
    {
        if (!showBlockedReason)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(fallbackBlockedReason))
        {
            return fallbackBlockedReason;
        }

        return "You can't do that.";
    }
}
