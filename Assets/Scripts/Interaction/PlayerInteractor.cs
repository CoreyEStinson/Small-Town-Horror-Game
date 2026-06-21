using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerInteractor : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Scene References")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private DialogueFlagStore flagStore;
    [SerializeField] private InteractionPromptController promptController;

    [Header("Selection")]
    [SerializeField] private float promptRefreshInterval = 0f;

    private readonly List<IInteractable> nearbyCandidates = new List<IInteractable>();
    private InputAction interactAction;
    private IInteractable currentTarget;
    private IInteractable currentBlockedTarget;
    private string currentBlockedReason;
    private float lastPromptRefreshTime;
    private string cachedInteractBindingDisplay;

    private void Awake()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        if (flagStore == null)
        {
            flagStore = FindAnyObjectByType<DialogueFlagStore>();
        }
    }

    private void Start()
    {
        if (inputActions == null)
        {
            Debug.LogError("Input Actions asset not assigned!", this);
            return;
        }

        InputActionMap playerActionMap = inputActions.FindActionMap("Player");
        if (playerActionMap == null)
        {
            Debug.LogError("Player action map not found in Input Actions!", this);
            return;
        }

        interactAction = playerActionMap.FindAction("Interact");
        if (interactAction == null)
        {
            Debug.LogError("Interact action not found", this);
            return;
        }

        playerActionMap.Enable();
        interactAction.started += OnInteract;
        cachedInteractBindingDisplay = ResolveInteractBindingDisplay();
        lastPromptRefreshTime = -promptRefreshInterval;
    }

    private void Update()
    {
        if (promptRefreshInterval > 0f)
        {
            if (Time.time - lastPromptRefreshTime < promptRefreshInterval)
            {
                return;
            }

            lastPromptRefreshTime = Time.time;
        }

        RefreshCurrentTarget();
        UpdatePrompt();
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
            dialogueRunner.HandleAdvanceInput();
            return;
        }

        RefreshCurrentTarget();

        if (currentTarget != null)
        {
            InteractionContext interactionContext = BuildContext();
            currentTarget.Interact(interactionContext);
            RefreshCurrentTarget();
            UpdatePrompt();
            return;
        }

        if (currentBlockedTarget != null &&
            !string.IsNullOrWhiteSpace(currentBlockedReason) &&
            promptController != null)
        {
            // Blocked feedback is intentionally deferred until Step 21.
        }
    }

    public void RegisterCandidate(IInteractable interactable)
    {
        if (interactable == null || nearbyCandidates.Contains(interactable))
        {
            return;
        }

        nearbyCandidates.Add(interactable);
    }

    public void UnregisterCandidate(IInteractable interactable)
    {
        if (interactable == null)
        {
            return;
        }

        nearbyCandidates.Remove(interactable);

        if (currentTarget == interactable)
        {
            currentTarget = null;
        }

        if (currentBlockedTarget == interactable)
        {
            currentBlockedTarget = null;
            currentBlockedReason = null;
        }
    }

    private void RefreshCurrentTarget()
    {
        currentTarget = null;
        currentBlockedTarget = null;
        currentBlockedReason = null;

        RemoveInvalidCandidates();

        if (TryGetBestValidTarget(out IInteractable interactable))
        {
            currentTarget = interactable;
            return;
        }

        if (TryGetBestBlockedTarget(out IInteractable blockedInteractable, out string failureReason))
        {
            currentBlockedTarget = blockedInteractable;
            currentBlockedReason = failureReason;
        }
    }

    private bool TryGetBestValidTarget(out IInteractable interactable)
    {
        interactable = null;
        float closestDistance = float.MaxValue;
        InteractionContext interactionContext = BuildContext();

        for (int i = 0; i < nearbyCandidates.Count; i++)
        {
            IInteractable candidate = nearbyCandidates[i];
            if (candidate == null || !candidate.CanInteract(interactionContext, out _))
            {
                continue;
            }

            float distance = GetDistanceTo(candidate);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                interactable = candidate;
            }
        }

        return interactable != null;
    }

    private bool TryGetBestBlockedTarget(out IInteractable interactable, out string failureReason)
    {
        interactable = null;
        failureReason = null;
        float closestDistance = float.MaxValue;
        InteractionContext interactionContext = BuildContext();

        for (int i = 0; i < nearbyCandidates.Count; i++)
        {
            IInteractable candidate = nearbyCandidates[i];
            if (candidate == null || candidate.CanInteract(interactionContext, out string reason))
            {
                continue;
            }

            float distance = GetDistanceTo(candidate);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                interactable = candidate;
                failureReason = reason;
            }
        }

        return interactable != null;
    }

    private float GetDistanceTo(IInteractable interactable)
    {
        if (interactable == null)
        {
            return float.MaxValue;
        }

        return Vector3.Distance(transform.position, interactable.GetInteractionPoint());
    }

    private InteractionContext BuildContext(Collider sourceCollider = null)
    {
        return new InteractionContext
        {
            interactor = gameObject,
            interactorTransform = transform,
            dialogueRunner = dialogueRunner,
            flagStore = flagStore,
            sourceCollider = sourceCollider
        };
    }

    private void UpdatePrompt()
    {
        if (promptController == null)
        {
            return;
        }

        if (currentTarget == null)
        {
            promptController.Hide();
            return;
        }

        string actionText = currentTarget.GetPrompt(BuildContext());
        string promptText = string.IsNullOrWhiteSpace(cachedInteractBindingDisplay)
            ? actionText
            : $"{cachedInteractBindingDisplay} - {actionText}";

        promptController.Show(promptText);
    }

    private string ResolveInteractBindingDisplay()
    {
        if (interactAction == null)
        {
            return string.Empty;
        }

        return interactAction.GetBindingDisplayString(group: "Keyboard&Mouse");
    }

    private void RemoveInvalidCandidates()
    {
        nearbyCandidates.RemoveAll(candidate =>
        {
            if (candidate == null)
            {
                return true;
            }

            MonoBehaviour behaviour = candidate as MonoBehaviour;
            return behaviour != null && (!behaviour.gameObject.activeInHierarchy || !behaviour.enabled);
        });
    }
}


