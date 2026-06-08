using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public static class DialogueYamlParser
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static NpcDialogueData Parse(string yamlText)
    {
        if (string.IsNullOrWhiteSpace(yamlText))
        {
            throw new ArgumentException("YAML text cannot be null or empty.", nameof(yamlText));
        }

        NpcDialogueData data = Deserializer.Deserialize<NpcDialogueData>(yamlText);
        Validate(data);
        return data;
    }

    public static NpcDialogueData Parse(TextAsset yamlAsset)
    {
        if (yamlAsset == null)
        {
            throw new ArgumentNullException(nameof(yamlAsset));
        }

        return Parse(yamlAsset.text);
    }

    public static bool TryParse(TextAsset yamlAsset, out NpcDialogueData data, out string error)
    {
        data = null;
        error = null;

        if (yamlAsset == null)
        {
            error = "YAML asset is null.";
            return false;
        }

        try
        {
            data = Parse(yamlAsset);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void Validate(NpcDialogueData data)
    {
        if (data == null)
        {
            throw new InvalidOperationException("Parsed dialogue data is null.");
        }

        if (string.IsNullOrWhiteSpace(data.npcId))
        {
            throw new InvalidOperationException("Dialogue YAML is missing npcId.");
        }

        if (data.conversations == null || data.conversations.Count == 0)
        {
            throw new InvalidOperationException($"NPC '{data.npcId}' does not define any conversations.");
        }

        foreach (ConversationData conversation in data.conversations)
        {
            ValidateConversation(data.npcId, conversation);
        }
    }

    private static void ValidateConversation(string npcId, ConversationData conversation)
    {
        if (conversation == null)
        {
            throw new InvalidOperationException($"NPC '{npcId}' has a null conversation entry.");
        }

        if (string.IsNullOrWhiteSpace(conversation.id))
        {
            throw new InvalidOperationException($"NPC '{npcId}' has a conversation with no id.");
        }

        if (conversation.lines == null || conversation.lines.Count == 0)
        {
            throw new InvalidOperationException(
                $"Conversation '{conversation.id}' on NPC '{npcId}' does not define any lines.");
        }

        Dictionary<string, LineData> lineLookup = new Dictionary<string, LineData>(StringComparer.Ordinal);

        foreach (LineData line in conversation.lines)
        {
            if (line == null)
            {
                throw new InvalidOperationException(
                    $"Conversation '{conversation.id}' on NPC '{npcId}' contains a null line.");
            }

            if (string.IsNullOrWhiteSpace(line.lineId))
            {
                continue;
            }

            if (!lineLookup.TryAdd(line.lineId, line))
            {
                throw new InvalidOperationException(
                    $"Conversation '{conversation.id}' on NPC '{npcId}' has duplicate lineId '{line.lineId}'.");
            }
        }

        if (!string.IsNullOrWhiteSpace(conversation.startLineId)
            && !lineLookup.ContainsKey(conversation.startLineId))
        {
            throw new InvalidOperationException(
                $"Conversation '{conversation.id}' on NPC '{npcId}' startLineId '{conversation.startLineId}' was not found.");
        }

        foreach (LineData line in conversation.lines)
        {
            ValidateLine(npcId, conversation.id, line, lineLookup);
        }
    }

    private static void ValidateLine(
        string npcId,
        string conversationId,
        LineData line,
        IReadOnlyDictionary<string, LineData> lineLookup)
    {
        bool hasChoices = line.choices != null && line.choices.Count > 0;
        bool hasNext = !string.IsNullOrWhiteSpace(line.nextLineId);

        if (hasChoices && hasNext)
        {
            throw new InvalidOperationException(
                $"Line '{line.lineId}' in conversation '{conversationId}' on NPC '{npcId}' defines both choices and nextLineId.");
        }

        if (hasNext && !lineLookup.ContainsKey(line.nextLineId))
        {
            throw new InvalidOperationException(
                $"Line '{line.lineId}' in conversation '{conversationId}' on NPC '{npcId}' points to missing nextLineId '{line.nextLineId}'.");
        }

        if (!hasChoices)
        {
            return;
        }

        foreach (ChoiceData choice in line.choices.Where(choice => choice != null))
        {
            if (string.IsNullOrWhiteSpace(choice.nextLineId))
            {
                throw new InvalidOperationException(
                    $"A choice on line '{line.lineId}' in conversation '{conversationId}' on NPC '{npcId}' is missing nextLineId.");
            }

            if (!lineLookup.ContainsKey(choice.nextLineId))
            {
                throw new InvalidOperationException(
                    $"Choice '{choice.choiceText}' on line '{line.lineId}' in conversation '{conversationId}' on NPC '{npcId}' points to missing nextLineId '{choice.nextLineId}'.");
            }
        }
    }
}
