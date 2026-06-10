using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInteractionController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Interaction")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private NpcInteraction currentInteractableNpc;
    [SerializeField] private InteractionPromptController interactionText;

    private InputAction interactAction;

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

        var playerActionMap = inputActions.FindActionMap("Player");
        if (playerActionMap == null)
        {
            Debug.LogError("Player action map not found in Input Actions!");
            return;
        }

        interactAction = playerActionMap.FindAction("Interact");
        if (interactAction == null)
        {
            Debug.LogError("Interact action not found");
            return;
        }

        playerActionMap.Enable();
        interactAction.started += OnInteract;
    }

    private void OnDestroy()
    {
        if (interactAction != null)
        {
            interactAction.started -= OnInteract;
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (dialogueRunner == null)
        {
            Debug.LogWarning("PlayerInteractionController.OnInteract: DialogueRunner not found!");
            return;
        }

        if (dialogueRunner.IsDialogueOpen)
        {
            dialogueRunner.HandleAdvanceInput();
            return;
        }

        if (currentInteractableNpc != null)
        {
            bool opened = dialogueRunner.BeginDialogue(currentInteractableNpc.dialogueLoader);
            Debug.Log("BeginDialogue returned: " + opened);
            Debug.Log("IsDialogueOpen after BeginDialogue: " + dialogueRunner.IsDialogueOpen);

            if (opened)
            {
                interactionText.Hide();
            }
        }
    }

    // TODO: Does not handle the situation where there are multiple NPCs with overlaping collisions
    // Should choose the closest NPC over the currently overlaping ones
    public void EnterInteractRange(NpcInteraction npcInteraction)
    {
        currentInteractableNpc = npcInteraction;
        interactionText.Show(npcInteraction.promptText);
    }

    public void ExitInteractRange(NpcInteraction npcInteraction)
    {
        currentInteractableNpc = null;
        interactionText.Hide();
    }

}
