using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class InteractionOption
{
    [Header("Prompt")]
    [TextArea]
    public string promptText = "Interact";

    [Header("Conditions")]
    public List<string> requiredFlags = new List<string>();
    public List<string> forbiddenFlags = new List<string>();

    [Header("Results")]
    public List<string> flagsToSet = new List<string>();

    [Header("Journal Results")]
    public List<JournalAction> journalActions =
        new List<JournalAction>();

    public bool runOnce;

    public UnityEvent onInteract = new UnityEvent();

    [NonSerialized] private bool hasBeenUsed;

    public bool isValid(GameState gameState)
    {
        if (hasBeenUsed || gameState == null)
        {
            return false;
        }

        foreach (string flag in requiredFlags)
        {
            if (!gameState.HasFlag(flag))
            {
                return false;
            }
        }

        foreach (string flag in forbiddenFlags)
        {
            if (gameState.HasFlag(flag))
            {
                return false;
            }
        }

        return true;
    }

    public void Execute(GameState gameState)
    {
        if (gameState == null)
        {
            Debug.LogError(
                "InteractionOption cannot execute without GameState."
            );

            return;
        }

        gameState.SetFlags(flagsToSet);

        JournalManager journalManager = JournalManager.Instance;

        foreach (JournalAction action in journalActions)
        {
            action?.Execute(journalManager);
        }

        if (runOnce)
        {
            hasBeenUsed = true;
        }

        onInteract.Invoke();
    }
}
