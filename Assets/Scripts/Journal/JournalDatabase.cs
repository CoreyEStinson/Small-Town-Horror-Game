using System;
using System.Collections.Generic;
using UnityEngine;
public sealed class JournalDatabase
{
    public bool IsLoaded {get; private set; } 

    public IReadOnlyCollection<Quest> Quests => questsById.Values; 
    public IReadOnlyCollection<Note> Notes => notesById.Values;

    private readonly Dictionary<string, Quest> questsById = 
        new Dictionary<string, Quest>(); 
    private readonly Dictionary<string, Note> notesById = 
        new Dictionary<string, Note>(); 
    
    private const string QuestResourcesPath = "Journal/Quests";
    private const string NoteResourcesPath = "Journal/Notes";

    public void LoadFromResources()
    {
        Clear();

        LoadFolder(QuestResourcesPath);
        LoadFolder(NoteResourcesPath);

        ValidateDatabase();
        IsLoaded = true;
    }

    private void Clear()
    {
        IsLoaded = false;

        questsById.Clear();
        notesById.Clear();
    }

    private void LoadFolder(string resourcesPath)
    {
        TextAsset[] yamlFiles = 
            Resources.LoadAll<TextAsset>(resourcesPath);

        if (yamlFiles.Length == 0)
        {
            Debug.LogWarning(
                $"No Journal YAML files found in Resources/{resourcesPath}."
            );

            return;
        }

        foreach (TextAsset yamlFile in yamlFiles)
        {
            if (!JournalYamlParser.TryParse(
                yamlFile,
                out JournalData data,
                out string error))
            {
                Debug.LogError(
                    $"Failed to parse Journal YAML '{yamlFile.name}': {error}"
                );

                continue;
            }

            AddJournalData(data, yamlFile.name);
        }
    }

    private void AddJournalData(JournalData data, string sourceName)
    {
        if (data.quests != null)
        {
            foreach (Quest quest in data.quests)
            {
                AddQuest(quest, sourceName);
            }
        }

        if (data.notes != null)
        {
            foreach (Note note in data.notes)
            {
                AddNote(note, sourceName);
            }
        }

    }

    private void ValidateDatabase()
    {
        if (questsById.Count == 0)
        {
            throw new InvalidOperationException(
            "Journal database does not contain any quests."
            );
        }

        int autoStartQuestCounter = 0;

        foreach (Quest quest in Quests)
        {
            if (quest.autoStart)
            {
                autoStartQuestCounter++;
            }
        }

        if (autoStartQuestCounter != 1)
        {
            throw new InvalidOperationException(
            "Journal database must define exactly one autoStart quest."
            );
        }
    }

    public bool TryGetQuest(string questId, out Quest quest)
    {
        return questsById.TryGetValue(questId, out quest);
    }

    public bool TryGetNote(string noteId, out Note note)
    {
        return notesById.TryGetValue(noteId, out note);
    }

    public bool TryGetObjective(string questId, string objectiveId, out Objective objective)
    {
        if (!TryGetQuest(questId, out Quest quest))
        {
            objective = null;
            return false;
        }

        foreach (Objective currentObjective in quest.objectives)
        {
            if (currentObjective.id == objectiveId)
            {
                objective = currentObjective;
                return true;
            }
        }

        objective = null;
        return false;
    }

    public Quest GetAutoStartQuest()
    {
        foreach(Quest quest in Quests)
        {
            if (quest.autoStart)
            {
                return quest;
            }
        }

        return null;
    }

    private void AddQuest(Quest quest, string sourceName)
    {
        if (quest == null)
        {
            throw new InvalidOperationException(
                $"Journal YAML '{sourceName}' contains a null quest entry."
            );
        }

        if (string.IsNullOrWhiteSpace(quest.id))
        {
            throw new InvalidOperationException(
                $"Journal YAML '{sourceName}' contains a quest with an empty id."
            );
        }

        if (questsById.ContainsKey(quest.id))
        {
            throw new InvalidOperationException(
                $"Duplicate quest id '{quest.id}' found in Journal YAML '{sourceName}'."
            );
        }

        questsById.Add(quest.id, quest);
    }

    private void AddNote(Note note, string sourceName)
    {
        if (note == null)
        {
            throw new InvalidOperationException(
                $"Journal YAML '{sourceName}' contains a null note entry."
            );
        }

        if (string.IsNullOrWhiteSpace(note.id))
        {
            throw new InvalidOperationException(
                $"Journal YAML '{sourceName}' contains a note with an empty id."
            );
        }

        if (notesById.ContainsKey(note.id))
        {
            throw new InvalidOperationException(
                $"Duplicate note id '{note.id}' found in Journal YAML '{sourceName}'."
            );
        }

        notesById.Add(note.id, note);
    }
}