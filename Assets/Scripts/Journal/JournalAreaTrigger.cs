using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class JournalAreaTrigger : MonoBehaviour
{
    [Header("Identify")]
    [SerializeField] private string triggerId;

    [Header("Actions")]
    [SerializeField] private List<JournalAction> actions = new List<JournalAction>();

    private void OnTriggerEnter(Collider other)
    {
        PlayerInteractionController player = 
            other.GetComponent<PlayerInteractionController>();

        if (player == null)
        {
            return;
        }

        JournalManager journalManager = JournalManager.Instance;

        if (journalManager == null ||
            !journalManager.IsInitialized ||
            journalManager.HasUsedTrigger(triggerId))
        {
            return;
        }

        foreach (JournalAction action in actions)
        {
            action?.Execute(journalManager);
        }

        journalManager.MarkTriggerUsed(triggerId);
    }
}