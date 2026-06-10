using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DialogueRunner : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private TextboxController textbox;
    [SerializeField] private DialogueFlagStore flagStore;

    private NpcDialogueData activeNpcData;
    private ConversationData activeConversation;
    private Dictionary<string, LineData> activeLineLookup;
    private List<LineData> activeLines;
    private LineData currentLine;
    private int currentLineIndex = -1;

    public bool IsDialogueOpen => activeConversation != null && textbox != null && textbox.IsOpen;

    private void Awake()
    {
        if (textbox == null)
        {
            textbox = FindTextboxController();
        }

        if (flagStore == null)
        {
            flagStore = FindAnyObjectByType<DialogueFlagStore>();
        }
    }

    private void OnEnable()
    {
        if (textbox != null)
        {
            textbox.ChoiceSelected += OnChoiceSelected;
        }
    }

    private void OnDisable()
    {
        if (textbox != null)
        {
            textbox.ChoiceSelected -= OnChoiceSelected;
        }
    }

    public bool BeginDialogue(DialogueYamlLoader loader)
    {
        if (loader == null)
        {
            Debug.LogError("DialogueRunner.BeginDialogue: loader parameter is null.", this);
            return false;
        }

        if (loader.DialogueData == null)
        {
            Debug.LogWarning("Loading dialogue data");
            loader.Load();
        }

        if (loader.DialogueData == null)
        {
            Debug.LogError($"DialogueRunner.BeginDialogue: Failed to load dialogue data from {loader}.", loader);
            return false;
        }

        ConversationData conversation = SelectConversation(loader.DialogueData);
        if (conversation == null)
        {
            Debug.LogWarning($"No valid conversation found for NPC '{loader.DialogueData.npcId}'.", loader);
            return false;
        }

        activeNpcData = loader.DialogueData;
        activeConversation = conversation;
        activeLineLookup = BuildLineLookup(conversation);
        activeLines = conversation.lines;

        if (flagStore != null)
        {
            flagStore.SetFlags(conversation.setFlags);
        }

        return ShowStartLine();
    }

    public bool Advance()
    {
        if (!IsDialogueOpen || currentLine == null)
        {
            return false;
        }

        bool hasChoices = currentLine.choices != null && currentLine.choices.Count > 0;
        if (hasChoices)
        {
            return false;
        }

        return ShowNextLineFromCurrent();
    }

    public bool HandleAdvanceInput()
    {
        if (!IsDialogueOpen)
        {
            return false;
        }

        if (textbox != null && textbox.IsTyping)
        {
            textbox.CompleteTyping();
            return true;
        }

        return Advance();
    }

    public void EndDialogue()
    {
        currentLine = null;
        currentLineIndex = -1;
        activeLines = null;
        activeLineLookup = null;
        activeConversation = null;
        activeNpcData = null;

        if (textbox != null)
        {
            textbox.Hide();
        }
    }

    private ConversationData SelectConversation(NpcDialogueData npcData)
    {
        if (npcData?.conversations == null || npcData.conversations.Count == 0)
        {
            Debug.LogWarning($"DialogueRunner.SelectConversation: NPC '{npcData?.npcId ?? "UNKNOWN"}' has no conversations.", this);
            return null;
        }

        List<ConversationData> sortedConversations = new List<ConversationData>(npcData.conversations);
        sortedConversations.Sort((left, right) => right.priority.CompareTo(left.priority));

        foreach (ConversationData conversation in sortedConversations)
        {
            if (conversation == null)
            {
                Debug.LogWarning($"DialogueRunner.SelectConversation: Found null conversation for NPC '{npcData.npcId}'.", this);
                continue;
            }

            if (flagStore == null)
            {
                Debug.LogWarning("DialogueRunner.SelectConversation: flagStore is null. Cannot validate conversation conditions.", this);
                return conversation;
            }

            if (flagStore.MeetsAll(conversation.requiresFlags))
            {
                return conversation;
            }
        }

        Debug.LogWarning($"DialogueRunner.SelectConversation: No conversation found for NPC '{npcData.npcId}' that meets all flag requirements.", this);
        return null;
    }

    private Dictionary<string, LineData> BuildLineLookup(ConversationData conversation)
    {
        Dictionary<string, LineData> lookup = new Dictionary<string, LineData>(StringComparer.Ordinal);

        if (conversation?.lines == null)
        {
            Debug.LogError("DialogueRunner.BuildLineLookup: Conversation has no lines.", this);
            return lookup;
        }

        int nullLineCount = 0;
        int emptyIdCount = 0;

        for (int i = 0; i < conversation.lines.Count; i++)
        {
            LineData line = conversation.lines[i];
            if (line == null)
            {
                nullLineCount++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line.lineId))
            {
                emptyIdCount++;
                continue;
            }

            lookup[line.lineId] = line;
        }

        if (nullLineCount > 0)
        {
            Debug.LogWarning($"DialogueRunner.BuildLineLookup: Found {nullLineCount} null line(s) in conversation.", this);
        }

        return lookup;
    }

    private bool ShowStartLine()
    {
        if (activeLines == null || activeLines.Count == 0)
        {
            Debug.LogError("DialogueRunner.ShowStartLine: Active conversation has no lines.", this);
            EndDialogue();
            return false;
        }

        if (string.IsNullOrWhiteSpace(activeConversation.startLineId))
        {
            return ShowLineAtIndex(0);
        }

        return ShowLineById(activeConversation.startLineId);
    }

    private bool ShowLineById(string lineId)
    {
        if (activeLineLookup == null)
        {
            Debug.LogError("DialogueRunner.ShowLineById: activeLineLookup is null.", this);
            EndDialogue();
            return false;
        }

        if (string.IsNullOrWhiteSpace(lineId))
        {
            Debug.LogError("DialogueRunner.ShowLineById: lineId is null or empty.", this);
            EndDialogue();
            return false;
        }

        if (!activeLineLookup.TryGetValue(lineId, out LineData nextLine) || nextLine == null)
        {
            Debug.LogError($"Missing lineId '{lineId}' in active conversation.", this);
            EndDialogue();
            return false;
        }

        int lineIndex = activeLines.IndexOf(nextLine);
        return ShowLineAtIndex(lineIndex);
    }

    private void OnChoiceSelected(ChoiceData choice)
    {
        if (choice == null)
        {
            Debug.LogWarning("DialogueRunner.OnChoiceSelected: choice parameter is null.", this);
            return;
        }

        if (flagStore != null && !flagStore.MeetsAll(choice.requiredConditions))
        {
            Debug.LogWarning("DialogueRunner.OnChoiceSelected: Choice does not meet required conditions.", this);
            return;
        }

        if (flagStore == null && (choice.requiredConditions?.Count ?? 0) > 0)
        {
            Debug.LogWarning("DialogueRunner.OnChoiceSelected: flagStore is null but choice has required conditions.", this);
        }

        if (string.IsNullOrWhiteSpace(choice.nextLineId))
        {
            Debug.Log("DialogueRunner.OnChoiceSelected: Choice leads to end of dialogue.", this);
            EndDialogue();
            return;
        }

        ShowLineById(choice.nextLineId);
    }

    private bool ShowNextLineFromCurrent()
    {
        if (currentLine == null)
        {
            Debug.LogError("DialogueRunner.ShowNextLineFromCurrent: currentLine is null.", this);
            EndDialogue();
            return false;
        }

        if (!string.IsNullOrWhiteSpace(currentLine.nextLineId))
        {
            return ShowLineById(currentLine.nextLineId);
        }

        return ShowLineAtIndex(currentLineIndex + 1);
    }

    private bool ShowLineAtIndex(int lineIndex)
    {
        if (activeLines == null)
        {
            Debug.LogError("DialogueRunner.ShowLineAtIndex: activeLines is null.", this);
            EndDialogue();
            return false;
        }

        if (lineIndex < 0 || lineIndex >= activeLines.Count)
        {
            Debug.LogWarning($"DialogueRunner.ShowLineAtIndex: lineIndex {lineIndex} out of range [0-{activeLines.Count - 1}]. Ending dialogue.", this);
            EndDialogue();
            return false;
        }

        LineData nextLine = activeLines[lineIndex];
        if (nextLine == null)
        {
            Debug.LogError($"DialogueRunner.ShowLineAtIndex: Line at index {lineIndex} is null.", this);
            EndDialogue();
            return false;
        }

        currentLineIndex = lineIndex;
        currentLine = nextLine;
        textbox.ShowLine(activeNpcData, BuildDisplayLine(currentLine));
        return true;
    }

    private LineData BuildDisplayLine(LineData sourceLine)
    {
        if (sourceLine == null)
        {
            Debug.LogError("DialogueRunner.BuildDisplayLine: sourceLine is null.", this);
            return new LineData();
        }

        LineData displayLine = new LineData
        {
            lineId = sourceLine.lineId,
            speakerOverride = sourceLine.speakerOverride,
            portrait = sourceLine.portrait,
            bodyText = sourceLine.bodyText,
            nextLineId = sourceLine.nextLineId
        };

        if (sourceLine.choices == null || sourceLine.choices.Count == 0)
        {
            return displayLine;
        }

        for (int i = 0; i < sourceLine.choices.Count; i++)
        {
            ChoiceData choice = sourceLine.choices[i];
            if (choice == null)
            {
                continue;
            }

            if (flagStore != null && !flagStore.MeetsAll(choice.requiredConditions))
            {
                continue;
            }

            displayLine.choices.Add(choice);
        }

        return displayLine;
    }

    private TextboxController FindTextboxController()
    {
        TextboxController[] textboxes = Resources.FindObjectsOfTypeAll<TextboxController>();
        if (textboxes.Length == 0)
        {
            Debug.LogError("DialogueRunner.FindTextboxController: No TextboxController found in scene.", this);
            return null;
        }

        for (int i = 0; i < textboxes.Length; i++)
        {
            TextboxController candidate = textboxes[i];
            if (candidate != null && candidate.gameObject.scene.IsValid())
            {
                return candidate;
            }
        }

        Debug.LogError("DialogueRunner.FindTextboxController: Found TextboxController(s) but none are in a valid scene.", this);
        return null;
    }
}
