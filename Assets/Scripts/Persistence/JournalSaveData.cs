using System;
using System.Collections.Generic;

[Serializable]
public class JournalSaveData
{
    public List<QuestSaveData> quests = new List<QuestSaveData>();
    public List<NoteSaveData> notes = new List<NoteSaveData>();
    public string trackedQuestId = string.Empty;
    public List<string> usedJournalTriggerIds = new List<string>();
    public int nextJournalOrder;
}

[Serializable]
public class QuestSaveData
{
    public string questId = string.Empty;
    public bool isCompleted;
    public int acquiredOrder;
    public int completedOrder = -1;
    public List<ObjectiveSaveData> objectives = new List<ObjectiveSaveData>();
}

[Serializable]
public class ObjectiveSaveData
{
    public string objectiveId = string.Empty;
    public bool isActive;
    public bool isCompleted;
    public int currentProgress;
}

[Serializable]
public class NoteSaveData
{
    public string noteId = string.Empty;
    public bool isRead;
    public int discoveryOrder;
}