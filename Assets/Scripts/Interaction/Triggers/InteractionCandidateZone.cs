using UnityEngine;

public sealed class InteractionCandidateZone : MonoBehaviour
{
    [SerializeField] private MonoBehaviour interactableSource;
    [SerializeField] private bool autoFindInParent = true;

    private IInteractable interactable;

    private void Awake()
    {
        if (!TryResolveInteractable())
        {
            Debug.LogError($"{name}: InteractionCandidateZone requires an IInteractable. Assign interactableSource or ensure an IInteractable exists on this object or a parent.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInteractor playerInteractor = GetPlayerInteractor(other);
        if (playerInteractor == null || interactable == null)
        {
            return;
        }

        playerInteractor.RegisterCandidate(interactable);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInteractor playerInteractor = GetPlayerInteractor(other);
        if (playerInteractor == null || interactable == null)
        {
            return;
        }

        playerInteractor.UnregisterCandidate(interactable);
    }

    private bool TryResolveInteractable()
    {
        interactable = interactableSource as IInteractable;
        if (interactable != null)
        {
            return true;
        }

        if (!autoFindInParent)
        {
            return false;
        }

        MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IInteractable resolvedInteractable)
            {
                interactable = resolvedInteractable;
                return true;
            }
        }

        return false;
    }

    private PlayerInteractor GetPlayerInteractor(Collider other)
    {
        PlayerInteractor playerInteractor = other.GetComponent<PlayerInteractor>();
        if (playerInteractor == null)
        {
            playerInteractor = other.GetComponentInParent<PlayerInteractor>();
        }

        return playerInteractor;
    }
}
