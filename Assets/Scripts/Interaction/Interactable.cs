using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Optional point used to determine which overlapping object is closest.")]
    [SerializeField] private Transform selectionPoint;

    [Header("Interaction Options")]
    [SerializeField] private List<InteractionOption> options = 
        new List<InteractionOption>();

    [Header("No Valid Option")]
    [SerializeField] private string disabledPrompt = "Can't use that.";
    [SerializeField] private UnityEvent onDisabledInteract = new UnityEvent();

    private DialogueFlagStore flagStore;

    private void Awake()
    {
        flagStore = FindAnyObjectByType<DialogueFlagStore>();

        if (flagStore == null)
        {
            Debug.LogError("Interactable needs a DialogueFlagStore in the scene", this);
        }
    }

    public Vector3 SelectionPosition => 
        selectionPoint != null ? selectionPoint.position : transform.position;

    public string GetCurrentPrompt()
    {
        return TryGetActiveOption(out InteractionOption option) 
            ? option.promptText
            : disabledPrompt;
    }

    public void Interact()
    {
        if (TryGetActiveOption(out InteractionOption option))
        {
            option.Execute(flagStore);
            return;
        }

        onDisabledInteract.Invoke();
    }

    public bool TryGetActiveOption(out InteractionOption activeOption)
    {
        activeOption = null;

        if (flagStore == null)
        {
            return false;
        }

        foreach (InteractionOption option in options)
        {
            if (option != null && option.isValid(flagStore))
            {
                activeOption = option;
                return true;
            }
        }

        return false;
    }
}
