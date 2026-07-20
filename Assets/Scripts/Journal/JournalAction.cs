using System;
using UnityEngine;

public enum JournalActionType
{
    StartQuest,
    ActivateObjective,
    CompleteObjective,
    AddObjectiveProgress,
    SetObjectiveProgress,
    CompleteQuest,
    SetTrackedQuest,
    DiscoverNote
}

[Serializable]
public class JournalAction
{
    [SerializeField] private JournalActionType actionType;
    
    [SerializeField] private string questId;
    [SerializeField] private string objectiveId;
    [SerializeField] private string noteId;
    
    [SerializeField] private int progressValue = 1;

    public bool Execute(JournalManager journalManager)
    {
        if (journalManager == null || !journalManager.IsInitialized)
        {
            return false;
        }

        switch (actionType)
        {
            case JournalActionType.StartQuest:
                return journalManager.StartQuest(questId);

            case JournalActionType.ActivateObjective:
                return journalManager.ActivateObjective(questId, objectiveId);

            case JournalActionType.CompleteObjective:
                return journalManager.CompleteObjective(questId, objectiveId);

            case JournalActionType.AddObjectiveProgress:
                return journalManager.AddObjectiveProgress(questId, objectiveId, progressValue);

            case JournalActionType.SetObjectiveProgress:
                return journalManager.SetObjectiveProgress(questId, objectiveId, progressValue);

            case JournalActionType.CompleteQuest:
                return journalManager.CompleteQuest(questId);

            case JournalActionType.SetTrackedQuest:
                return journalManager.SetTrackedQuest(questId);

            case JournalActionType.DiscoverNote:
                return journalManager.DiscoverNote(noteId);

            default:
                return false;
        }
    }
}