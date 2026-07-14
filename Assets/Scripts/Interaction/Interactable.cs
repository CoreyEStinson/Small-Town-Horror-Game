using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip(
        "Optional point used to determine which overlapping object is closest."
    )]
    [SerializeField] private Transform selectionPoint;

    [Header("Interaction Options")]
    [SerializeField] private List<InteractionOption> options =
        new List<InteractionOption>();

    [Header("No Valid Option")]
    [SerializeField] private string disabledPrompt = "Can't use that.";
    [SerializeField] private UnityEvent onDisabledInteract =
        new UnityEvent();

    private GameState gameState;

    private void Awake()
    {
        gameState = FindAnyObjectByType<GameState>();

        if (gameState == null)
        {
            Debug.LogError(
                "Interactable needs GameState in the scene.",
                this
            );
        }
    }

    public Vector3 SelectionPosition =>
        selectionPoint != null
            ? selectionPoint.position
            : transform.position;

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
            option.Execute(gameState);
            return;
        }

        onDisabledInteract.Invoke();
    }

    public bool TryGetActiveOption(out InteractionOption activeOption)
    {
        activeOption = null;

        if (gameState == null)
        {
            return false;
        }

        foreach (InteractionOption option in options)
        {
            if (option != null && option.isValid(gameState))
            {
                activeOption = option;
                return true;
            }
        }

        return false;
    }
}