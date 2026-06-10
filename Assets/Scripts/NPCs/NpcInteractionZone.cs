using UnityEngine;

public class NpcInteractionZone : MonoBehaviour
{
    [SerializeField] private NpcInteraction npcInteraction;

    private void Awake()
    {
        if (npcInteraction == null)
        {
            npcInteraction = GetComponentInParent<NpcInteraction>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerInteractionController playerInteraction =
            other.GetComponent<PlayerInteractionController>();

        if (playerInteraction == null || npcInteraction == null)
        {
            return;
        }

        playerInteraction.EnterInteractRange(npcInteraction);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerInteractionController playerInteraction =
            other.GetComponent<PlayerInteractionController>();

        if (playerInteraction == null || npcInteraction == null)
        {
            return;
        }

        playerInteraction.ExitInteractRange(npcInteraction);
    }


}
