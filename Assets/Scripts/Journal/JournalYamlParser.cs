using System;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public static class JournalYamlParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static JournalData Parse(string yamlText)
    {
        if (string.IsNullOrWhiteSpace(yamlText))
        {
            throw new ArgumentException("Yaml text cannot be null or empty.", nameof(yamlText));
        }

        JournalData data = Deserializer.Deserialize<JournalData>(yamlText);
        Validate(data);
        return data;
    }

    public static JournalData Parse(TextAsset yamlAsset)
    {
        if (yamlAsset == null)
        {
            throw new ArgumentNullException(nameof(yamlAsset));
        }

        return Parse(yamlAsset.text);
    }

    public static bool TryParse(TextAsset yamlAsset, out JournalData data, out string error)
    {
        data = null;
        error = null;

        if (yamlAsset == null)
        {
            error = "YAML asset is null";
            return false;
        }

        try
        {
            data = Parse(yamlAsset);
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }

    private static void Validate(JournalData data)
    {
        if (data == null)
        {
            throw new InvalidOperationException("Parsed journal data is null");
        }

        bool hasQuests = data.quests != null && data.quests.Count > 0;
        bool hasNotes = data.notes != null && data.notes.Count > 0;

        if (!hasQuests && !hasNotes)
        {
            throw new InvalidOperationException("Journal does not define any supported entries");
        }

        if (data.quests != null)
        {
            foreach (Quest quest in data.quests)
            {
                ValidateQuest(quest);
            }
        }

        if (data.notes != null)
        {
            foreach (Note note in data.notes)
            {
                ValidateNote(note);
            }
        }
    }

    private static void ValidateQuest(Quest quest)
    {
        if (quest == null)
        {
            throw new InvalidOperationException($"Journal has a null quest entry.");
        }

        if (string.IsNullOrWhiteSpace(quest.id))
        {
            throw new InvalidCastException("Journal has a quest with no id");
        }

        if (quest.type != Quest.QuestType.Main && quest.type != Quest.QuestType.Side)
        {
            throw new InvalidOperationException($"Quest '{quest.id}' in the journal does not have a quest type");
        }

        if (string.IsNullOrWhiteSpace(quest.title))
        {
            throw new InvalidOperationException($"Quest '{quest.id}' in the journal does not have a title");
        }

        if (string.IsNullOrWhiteSpace(quest.summary))
        {
            throw new InvalidOperationException($"Quest '{quest.id}' in the journal does not have a summary");
        }

        if (string.IsNullOrWhiteSpace(quest.completionSummary))
        {
            throw new InvalidOperationException($"Quest '{quest.id}' in the journal does not have a completion summary");
        }

        if (quest.objectives == null || quest.objectives.Count == 0)
        {
            throw new InvalidOperationException($"Quest '{quest.id}' in the journal does not define any objectives");
        }

        foreach (Objective objective in quest.objectives)
        {
            ValidateObjective(quest.id, objective);
        }
    }

    private static void ValidateObjective(string questId, Objective objective) 
    {
        if (objective == null)
        {
            throw new InvalidOperationException($"Quest '{questId}' has a null objective entry.");
        }

        if (string.IsNullOrWhiteSpace(objective.id))
        {
            throw new InvalidOperationException($"Quest '{questId}' has an objective with no id.");
        }
    }

    private static void ValidateNote(Note note)
    {
        if (note == null)
        {
            throw new InvalidOperationException("Journal has a null note entry");
        }

        if (string.IsNullOrWhiteSpace(note.id))
        {
            throw new InvalidOperationException("Journal has a note with no id");
        }

        if (string.IsNullOrWhiteSpace(note.title))
        {
            throw new InvalidOperationException($"Note '{note.id}' does not have a title");
        }

        if (!string.IsNullOrWhiteSpace(note.imagePath))
        {
            string imagePath = note.imagePath.Replace('\\', '/').Trim();

            Texture2D texture = Resources.Load<Texture2D>(imagePath);
            Sprite sprite = Resources.Load<Sprite>(imagePath);

            if (texture == null && sprite == null)
            {
                throw new InvalidOperationException($"Note '{note.id}' references imagePath '{note.imagePath}', but no image asset was found at that path.");
            }
        }
    }


}