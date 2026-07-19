using System;
using System.Collections.Generic;

[Serializable]
public sealed class JournalData
{
    public List<Quest> quests;
    public List<Note> notes;
}

[Serializable]
public sealed class Quest
{
    public enum QuestType
    {
        Main,
        Side
    } 
    public string id;
    public QuestType type;
    public string title;
    public string summary;
    public string completionSummary;
    public List<Objective> objectives;
}

[Serializable]
public sealed class Objective
{
    public string id;
    public string text;
    public string? location;
    public string? giver;
    public int? progressTarget;
    public bool initiallyActive;
}

[Serializable]
public sealed class Note
{
    public string id;
    public string title;
    public string? bodyText;
    public string? imagePath;
}