using System;
using System.Collections.Generic;

[Serializable]
public sealed class NpcDialogueData
{
    public string npcId;
    public string displayName;
    public string portrait;
    public List<ConversationData> conversations = new List<ConversationData>();
}

[Serializable]
public sealed class ConversationData
{
    public string id;
    public int priority;
    public List<string> requiresFlags = new List<string>();
    public List<string> setFlags = new List<string>();
    public string startLineId;
    public List<LineData> lines = new List<LineData>();
}

[Serializable]
public sealed class LineData
{
    public string lineId;
    public string speakerOverride;
    public string portrait;
    public string bodyText;
    public List<ChoiceData> choices = new List<ChoiceData>();
    public string nextLineId;
}

[Serializable]
public sealed class ChoiceData
{
    public string choiceText;
    public List<string> requiredConditions = new List<string>();
    public string nextLineId;
}
