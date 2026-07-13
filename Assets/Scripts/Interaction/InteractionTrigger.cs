using UnityEngine;

[RequireComponent(typeof(Collider))]
public class InteractionTrigger : MonoBehaviour
{
    [SerializeField] private Interactable interactable;

    private void Awake()
    {
        if (interactable == null)
        {
            interactable = GetComponentInParent<Interactable>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInteractionController player = 
            other.GetComponent<PlayerInteractionController>();

        if (player != null && interactable != null)
        {
            player.EnterInteractionRange(interactable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInteractionController player = 
            other.GetComponent<PlayerInteractionController>();

        if (player != null && interactable != null)
        {
            player.ExitInteractionRange(interactable);
        }  
    }
}
