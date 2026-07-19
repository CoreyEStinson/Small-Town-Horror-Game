using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInteractionController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Scene References")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private InteractionPromptController interactionPrompt;

    private readonly Dictionary<Interactable, int> nearbyInteractables =
        new Dictionary<Interactable, int>();

    private InputAction interactAction;
    private Interactable currentInteractable;
    private bool dialogueWasOpen;
    private bool transitionWasActive;

    private void Awake()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }
    }

    private void Start()
    {
        if (inputActions == null)
        {
            Debug.LogError("Input Actions asset not assigned!");
            return;
        }

        InputActionMap playerActionMap =
            inputActions.FindActionMap("Player");

        if (playerActionMap == null)
        {
            Debug.LogError("Player action map not found!");
            return;
        }

        interactAction = playerActionMap.FindAction("Interact");

        if (interactAction == null)
        {
            Debug.LogError("Interact action not found!");
            return;
        }

        playerActionMap.Enable();
        interactAction.started += OnInteract;
    }

    private void Update()
    {
        bool transitionIsActive =
            FadeTransition.IsTransitioning;

        if (transitionIsActive)
        {
            interactionPrompt?.Hide();
        }
        else if (transitionWasActive)
        {
            RefreshPrompt();
        }

        transitionWasActive = transitionIsActive;

        bool dialogueIsOpen =
            dialogueRunner != null &&
            dialogueRunner.IsDialogueOpen;

        if (dialogueWasOpen && !dialogueIsOpen)
        {
            RefreshPrompt();
        }

        dialogueWasOpen = dialogueIsOpen;
    }

    private void OnDestroy()
    {
        if (interactAction != null)
        {
            interactAction.started -= OnInteract;
        }
    }

    public void EnterInteractionRange(Interactable interactable)
    {
        if (interactable == null)
        {
            return;
        }

        if (nearbyInteractables.ContainsKey(interactable))
        {
            nearbyInteractables[interactable]++;
        }
        else
        {
            nearbyInteractables.Add(interactable, 1);
        }

        RefreshPrompt();
    }

    public void ExitInteractionRange(Interactable interactable)
    {
        if (interactable == null ||
            !nearbyInteractables.TryGetValue(
                interactable,
                out int overlapCount))
        {
            return;
        }

        overlapCount--;

        if (overlapCount <= 0)
        {
            nearbyInteractables.Remove(interactable);
        }
        else
        {
            nearbyInteractables[interactable] = overlapCount;
        }

        RefreshPrompt();
    }

    public void ResetInteractionRanges()
    {
        nearbyInteractables.Clear();
        currentInteractable = null;
        interactionPrompt?.Hide();
    }
    private void OnInteract(InputAction.CallbackContext context)
    {
        if (FadeTransition.IsTransitioning)
        {
            return;
        }

        if (dialogueRunner != null &&
            dialogueRunner.IsDialogueOpen)
        {
            dialogueRunner.HandleAdvanceInput();
            return;
        }

        if (currentInteractable == null)
        {
            return;
        }

        currentInteractable.Interact();
        RefreshPrompt();
    }

    private void RefreshPrompt()
    {
        if (interactionPrompt == null)
        {
            return;
        }

        if (FadeTransition.IsTransitioning ||
            (dialogueRunner != null &&
             dialogueRunner.IsDialogueOpen))
        {
            interactionPrompt.Hide();
            return;
        }

        currentInteractable = FindClosestInteractable();

        if (currentInteractable == null)
        {
            interactionPrompt.Hide();
            return;
        }

        interactionPrompt.Show(
            currentInteractable.GetCurrentPrompt()
        );
    }

    private Interactable FindClosestInteractable()
    {
        Interactable closest = null;
        float closestDistanceSqr = float.MaxValue;

        foreach (KeyValuePair<Interactable, int> entry
                 in nearbyInteractables)
        {
            Interactable candidate = entry.Key;

            if (candidate == null ||
                !candidate.isActiveAndEnabled ||
                entry.Value <= 0)
            {
                continue;
            }

            float distanceSqr =
                (candidate.SelectionPosition - transform.position)
                .sqrMagnitude;

            if (distanceSqr < closestDistanceSqr)
            {
                closest = candidate;
                closestDistanceSqr = distanceSqr;
            }
        }

        return closest;
    }
}
