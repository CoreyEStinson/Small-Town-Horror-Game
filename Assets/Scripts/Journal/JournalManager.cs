using System;
using UnityEngine;

public enum JournalUpdateType
{
    QuestStarted,
    ObjectiveActivated,
    ObjectiveCompleted,
    QuestCompleted
}

public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance { get; private set; } 

    public event Action JournalChanged;
    public event Action<JournalUpdateType, string> JournalToastRequested; 
    public event Action<Note> NoteDiscovered;

    public JournalSaveData Data => gameSession.JournalSaveData;

    public bool IsInitialized { get; private set; }

    private GameSession gameSession;
    private JournalDatabase database;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        gameSession = GetComponent<GameSession>();

        if (gameSession == null) {
            Debug.LogError(
                "JournalManager requires GameSession on the same GameObject.",
                this
            );

            return;
        }

        database = new JournalDatabase();

        try
        {
            database.LoadFromResources();
            IsInitialized = database.IsLoaded;
        }
        catch (Exception exception)
        {
            IsInitialized = false;
            Debug.LogError($"Failed to initialize journal database: {exception.Message}");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) 
        {
            Instance = null;
        }        
    }

    public void StartNewGame()
    {
        if (!CanModifyJournal())
        {
            return;
        }

        gameSession.SetJournalSaveData(new JournalSaveData());

        Quest autoStartQuest = database.GetAutoStartQuest();

        if (autoStartQuest == null) 
        {
            Debug.LogError(
                "Journal database has no auto start quest",
                this
            );

            return;
        }

        StartQuest(autoStartQuest.id);
    }

    public bool StartQuest(string questId)
    {
        if (!CanModifyJournal())
        {
            return false;
        }

        if (!database.TryGetQuest(questId, out Quest quest))
        {
            return false;
        }

        if (FindQuestState(questId) != null) 
        {
            return false;
        }

        QuestSaveData questSaveData = new QuestSaveData
        {
            questId = quest.id,
            acquiredOrder = GetNextJournalOrder()
        };        

        foreach (Objective objective in quest.objectives)
        {
            questSaveData.objectives.Add(new ObjectiveSaveData
            {
                objectiveId = objective.id,
                isActive = objective.initiallyActive,
                isCompleted = false,
                currentProgress = 0
            });
        }

        Data.quests.Add(questSaveData);

        if (string.IsNullOrWhiteSpace(Data.trackedQuestId))
        {
            Data.trackedQuestId = quest.id;
        }

        NotifyJournalUpdated(JournalUpdateType.QuestStarted, quest.title);
        return true;
    }

    public bool ActivateObjective(string questId, string objectiveId)
    {
        if (!CanModifyJournal() ||
            !database.TryGetObjective(questId, objectiveId, out Objective objective))
        {
            return false;
        }

        QuestSaveData questState = FindQuestState(questId);
        ObjectiveSaveData objectiveState = 
            FindObjectiveState(questState, objectiveId);
        
        if (questState == null ||
            questState.isCompleted ||
            objectiveState == null ||
            objectiveState.isActive ||
            objectiveState.isCompleted)
        {
            return false;
        }

        objectiveState.isActive = true;
        NotifyJournalUpdated(JournalUpdateType.ObjectiveActivated, objective.text);
        return true;
    }

    public bool CompleteObjective(string questId, string objectiveId)
    {
        if (!CanModifyJournal() ||
            !database.TryGetObjective(questId, objectiveId, out Objective objective))
        {
            return false;
        }

        QuestSaveData questState = FindQuestState(questId);
        ObjectiveSaveData objectiveState = 
            FindObjectiveState(questState, objectiveId);

        if (questState == null ||
            questState.isCompleted ||
            objectiveState == null ||
            objectiveState.isCompleted)
        {
            return false;
        }

        objectiveState.isActive = false;
        objectiveState.isCompleted = true;

        if (objective.progressTarget.HasValue)
        {
            objectiveState.currentProgress = objective.progressTarget.Value;
        }

        NotifyJournalUpdated(JournalUpdateType.ObjectiveCompleted, $"{objective.text} Complete");

        if (AreAllObjectivesCompleted(questState))
        {
            CompleteQuest(questId);
        }

        return true;
    }

    public bool AddObjectiveProgress(
        string questId,
        string objectiveId,
        int amount
    )
    {
        if (amount <= 0)
        {
            return false;
        }

        QuestSaveData questState = FindQuestState(questId);
        ObjectiveSaveData objectiveState = 
            FindObjectiveState(questState, objectiveId);

        if (objectiveState == null)
        {
            return false;
        }

        return SetObjectiveProgress(
            questId,
            objectiveId,
            objectiveState.currentProgress + amount
        );
    }

    public bool SetObjectiveProgress(
        string questId,
        string objectiveId,
        int value
    )
    {
        if (!CanModifyJournal() ||
            !database.TryGetObjective(questId, objectiveId, out Objective objective) ||
            !objective.progressTarget.HasValue ||
            objective.progressTarget.Value <= 0)
        {
            return false;
        }

        QuestSaveData questState = FindQuestState(questId);
        ObjectiveSaveData objectiveState = 
            FindObjectiveState(questState, objectiveId);
        
        if (questState == null ||
            questState.isCompleted ||
            objectiveState == null ||
            !objectiveState.isActive ||
            objectiveState.isCompleted)
        {
            return false;
        }

        int target = objective.progressTarget.Value;

        objectiveState.currentProgress = Mathf.Clamp(
            value,
            0,
            target
        );

        if (objectiveState.currentProgress >= target)
        {
            return CompleteObjective(questId, objectiveId);
        }

        NotifyJournalChanged();

        return true;
    }

    public bool CompleteQuest(string questId)
    {
        if (!CanModifyJournal() ||
            !database.TryGetQuest(questId, out Quest quest))
        {
            return false;
        }

        QuestSaveData questState = FindQuestState(questId);

        if (questState == null || questState.isCompleted)
        {
            return false;
        }

        foreach (ObjectiveSaveData objectiveState in questState.objectives)
        {
            if (!database.TryGetObjective(
                questId,
                objectiveState.objectiveId,
                out Objective objective))
            {
                continue;
            }

            objectiveState.isActive = false;
            objectiveState.isCompleted = true;

            if (objective.progressTarget.HasValue)
            {
                objectiveState.currentProgress =
                    objective.progressTarget.Value;
            }
        }

        questState.isCompleted = true;
        questState.completedOrder = GetNextJournalOrder();

        EnsureTrackedQuest();

        NotifyJournalUpdated(JournalUpdateType.QuestCompleted, quest.title);
        return true;
    }

    public bool SetTrackedQuest(string questId)
    {
        if (!CanModifyJournal())
        {
            return false;
        }

        QuestSaveData questState = FindQuestState(questId);

        if (questState == null || questState.isCompleted)
        {
            return false;
        }

        if (Data.trackedQuestId == questId)
        {
            return false;
        }

        Data.trackedQuestId = questId;
        NotifyJournalChanged();
        return true;
    }

    public bool DiscoverNote(string noteId)
    {
        if (!CanModifyJournal() ||
            !database.TryGetNote(noteId, out Note note))
        {
            return false;
        }

        if (FindNoteState(noteId) != null)
        {
            return false;
        }

        Data.notes.Add(new NoteSaveData
        {
            noteId = note.id,
            isRead = false,
            discoveryOrder = GetNextJournalOrder()
        });

        JournalChanged?.Invoke();
        NoteDiscovered?.Invoke(note);
        return true;
    }

    public bool MarkNoteRead(string noteId)
    {
        NoteSaveData noteState = FindNoteState(noteId);

        if (noteState == null || noteState.isRead)
        {
            return false;
        }

        noteState.isRead = true;
        NotifyJournalChanged();
        return true;
    }

    public bool HasUsedTrigger(string triggerId)
    {
        return !string.IsNullOrWhiteSpace(triggerId) && 
                Data.usedJournalTriggerIds.Contains(triggerId);
    }

    public bool MarkTriggerUsed(string triggerId)
    {
        if (string.IsNullOrWhiteSpace(triggerId) ||
            HasUsedTrigger(triggerId))
        {
            return false;
        }

        Data.usedJournalTriggerIds.Add(triggerId);
        return true;
    }

    public Objective GetCurrentObjective(string questId)
    {
        if (!CanModifyJournal() || 
            !database.TryGetQuest(questId, out Quest quest))
        {
            return null;
        }

        QuestSaveData questState = FindQuestState(questId);

        if (questState == null || questState.isCompleted)
        {
            return null;
        }

        foreach (Objective objective in quest.objectives)
        {
            ObjectiveSaveData objectiveState = 
                FindObjectiveState(questState, objective.id);
            
            if (objectiveState != null && 
                objectiveState.isActive &&
                !objectiveState.isCompleted)
            {
                return objective;
            }
        }

        return null;
    }

    public bool TryGetQuestDefinition(string questId, out Quest quest)
    {
        if (database == null)
        {
            quest = null;
            return false;
        }

        return database.TryGetQuest(questId, out quest);
    }

    public bool TryGetNoteDefinition(string noteId, out Note note)
    {
        if (database == null)
        {
            note = null;
            return false;
        }

        return database.TryGetNote(noteId, out note);
    }

    private bool CanModifyJournal()
    {
        return IsInitialized &&
                gameSession != null &&
                database != null;
    }

    private QuestSaveData FindQuestState(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
        {
            return null;
        }

        foreach (QuestSaveData questState in Data.quests)
        {
            if (questState.questId == questId)
            {
                return questState;
            }
        }

        return null;
    }

    private ObjectiveSaveData FindObjectiveState(
        QuestSaveData questState,
        string objectiveId)
    {
        if (questState == null ||
            string.IsNullOrWhiteSpace(objectiveId))
        {
            return null;
        }

        foreach (ObjectiveSaveData objectiveState in questState.objectives)
        {
            if (objectiveState.objectiveId == objectiveId)
            {
                return objectiveState;
            }
        }

        return null;
    }

    private NoteSaveData FindNoteState(string noteId)
    {
        if (string.IsNullOrWhiteSpace(noteId))
        {
            return null;
        }

        foreach (NoteSaveData noteState in Data.notes)
        {
            if (noteState.noteId == noteId)
            {
                return noteState;
            }
        }

        return null;
    }

    private bool AreAllObjectivesCompleted(QuestSaveData questState)
    {
        if (questState == null || questState.objectives.Count == 0)
        {
            return false;
        }

        foreach (ObjectiveSaveData objectiveState in questState.objectives)
        {
            if (!objectiveState.isCompleted)
            {
                return false;
            }
        }

        return true;
    }

    private int GetNextJournalOrder()
    {
        int order = Data.nextJournalOrder;
        Data.nextJournalOrder++;
        return order;
    }

    private void EnsureTrackedQuest()
    {
        QuestSaveData trackedQuest = 
            FindQuestState(Data.trackedQuestId);
        
        if (trackedQuest != null && !trackedQuest.isCompleted)
        {
            return;
        }

        QuestSaveData newestMainQuest = null;
        QuestSaveData newestSideQuest = null;

        foreach (QuestSaveData questState in Data.quests)
        {
            if (questState.isCompleted || 
                !database.TryGetQuest(questState.questId, out Quest quest))
            {
                continue;
            }

            if (quest.type == Quest.QuestType.Main &&
                (newestMainQuest == null || 
                 questState.acquiredOrder > newestMainQuest.acquiredOrder))
            {
                newestMainQuest = questState;
            }

            if (quest.type == Quest.QuestType.Side &&
                (newestSideQuest == null || 
                 questState.acquiredOrder > newestSideQuest.acquiredOrder))
            {
                newestSideQuest = questState;
            }
        }

        QuestSaveData replacementQuest = 
            newestMainQuest ?? newestSideQuest;

        Data.trackedQuestId = replacementQuest != null 
            ? replacementQuest.questId
            : string.Empty;
    }

    private void NotifyJournalChanged()
    {
        JournalChanged?.Invoke();
    }

    private void NotifyJournalUpdated(JournalUpdateType updateType, string updateText)
    {
        JournalChanged?.Invoke();
        JournalToastRequested?.Invoke(updateType, updateText);   
    } 

}
