using System;
using System.Collections.Generic;
using System.Text;

public sealed class DialogueTextEffectParser
{
    public DialogueProcessedText Parse(string rawText)
    {
        ParserState state = new ParserState();

        if (string.IsNullOrEmpty(rawText))
        {
            return new DialogueProcessedText(
                string.Empty,
                state.spans,
                state.warnings
            );
        }

        int i = 0;
        while (i < rawText.Length)
        {
            char ch = rawText[i];

            if (ch == '<')
            {
                int closeIndex = rawText.IndexOf('>', i);
                if (closeIndex == -1)
                {
                    AppendVisibleCharacter(ch, state);
                    i++;
                    continue;
                }

                string tagText = rawText.Substring(i, closeIndex - i + 1);
                
                if (string.Equals(tagText, "<wave>", StringComparison.OrdinalIgnoreCase))
                {
                    HandleOpenTag("wave", state);
                }
                else if (string.Equals(tagText, "</wave>", StringComparison.OrdinalIgnoreCase))
                {
                    HandleCloseTag("wave", state);
                }
                else
                {
                    AppendTmpTag(tagText, state);
                }

                i = closeIndex + 1;
                continue;
            }

            AppendVisibleCharacter(ch, state);
            i++;
        }

        // Any unclosed wave tags
        while (state.openEffects.Count > 0)
        {
            OpenEffect dangling = state.openEffects.Pop();
            Warn($"Unclosed <wave> tag starting at visible char {dangling.startVisibleCharacterIndex}."
            + "Showing text without wave for unfinished span", state);
        }

        return new DialogueProcessedText(
            state.output.ToString(),
            state.spans,
            state.warnings
        );
    }

    private void HandleOpenTag(string tagName, ParserState state)
    {
        if (tagName != "wave")
        {
            Warn($"Unsupported custom tag <{tagName}>.", state);
            return;
        }

        foreach (OpenEffect open in state.openEffects)
        {
            if (open.effectType == DialogueTextEffectType.Wave)
            {
                Warn("Nested <wave> tags are not supported. Ignoring inner <wave>", state);
                return;
            }
        }

        state.openEffects.Push(new OpenEffect
        {
            effectType = DialogueTextEffectType.Wave,
            startVisibleCharacterIndex = state.visibleCharacterCount
        });
    }

    private void HandleCloseTag(string tagName, ParserState state)
    {
        if (tagName != "wave")
        {
            Warn($"Unsupported custom closing tag </{tagName}>.", state);
            return;
        }

        if (state.openEffects.Count == 0)
        {
            Warn("Found </wave> without a matching <wave>. Ignoring it.", state);
            return;
        }

        OpenEffect open = state.openEffects.Pop();

        if (open.effectType != DialogueTextEffectType.Wave)
        {
            Warn("Mismatched closing </wave> tag", state);
            return;
        }

        int length = state.visibleCharacterCount - open.startVisibleCharacterIndex;
        if (length <= 0)
        {
            Warn("Empty <wave></wave> span found. Ignoring it.", state);
            return;
        }

        state.spans.Add(new DialogueTextEffectSpan
        {
            effectType = DialogueTextEffectType.Wave,
            startVisibleCharacterIndex = open.startVisibleCharacterIndex,
            length = length
        });
    }

    private void AppendVisibleCharacter(char ch, ParserState state)
    {
        state.output.Append(ch);
        state.visibleCharacterCount++;
    }

    private void AppendTmpTag(string tagText, ParserState state)
    {
        state.output.Append(tagText);
    }

    private void Warn(string message, ParserState state)
    {
        state.warnings.Add(message);
    }

    private class OpenEffect
    {
        public DialogueTextEffectType effectType;
        public int startVisibleCharacterIndex;
    }

    private class ParserState
    {
        public StringBuilder output = new();
        public int visibleCharacterCount;
        public Stack<OpenEffect> openEffects = new();
        public List<DialogueTextEffectSpan> spans = new();
        public List<string> warnings = new();
    }
}

public enum DialogueTextEffectType
{
    Wave
}

public struct DialogueProcessedText
{
    public string processedText;
    public List<DialogueTextEffectSpan> effectSpans;
    public List<string> warnings;

    public DialogueProcessedText(string processedText, List<DialogueTextEffectSpan> effectSpans, List<string> warnings)
    {
        this.processedText = processedText;
        this.effectSpans = effectSpans ?? new List<DialogueTextEffectSpan>();
        this.warnings = warnings ?? new List<string>();
    }
}

public struct DialogueTextEffectSpan
{
    public DialogueTextEffectType effectType;
    public int startVisibleCharacterIndex;
    public int length;

    public DialogueTextEffectSpan(DialogueTextEffectType effectType, int startVisibleCharacterIndex, int length)
    {
        this.effectType = effectType;
        this.startVisibleCharacterIndex = startVisibleCharacterIndex;
        this.length = length;
    }
}

