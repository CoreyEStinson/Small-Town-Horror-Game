using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInteractionController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Interaction")]
    [SerializeField] private float maxInteractionDistance = 1f;
    [SerializeField] private DialogueRunner dialogueRunner;

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

        interactAction.started += OnInteract;
        playerActionMap.Enable();
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
        if (dialogueRunner != null && dialogueRunner.IsDialogueOpen)
        {
            dialogueRunner.Advance();
            return;
        }

        GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
        if (npcs.Length == 0)
        {
            return;
        }

        DialogueYamlLoader nearestLoader = null;
        float nearestDistSqr = float.MaxValue;
        Vector3 pos = transform.position;

        foreach (GameObject npc in npcs)
        {
            DialogueYamlLoader loader = npc.GetComponent<DialogueYamlLoader>();
            if (loader == null)
            {
                continue;
            }

            float d = (npc.transform.position - pos).sqrMagnitude;
            if (d < nearestDistSqr)
            {
                nearestDistSqr = d;
                nearestLoader = loader;
            }
        }

        if (nearestLoader != null && nearestDistSqr <= Math.Pow(maxInteractionDistance, 2))
        {
            dialogueRunner?.BeginDialogue(nearestLoader);
        }
    }
}
