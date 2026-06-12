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
                
                if (string.Equals(tagText, "<shake>", StringComparison.OrdinalIgnoreCase))
                {
                    HandleOpenTag("shake", state);
                }
                else if (string.Equals(tagText, "</shake>", StringComparison.OrdinalIgnoreCase))
                {
                    HandleCloseTag("shake", state);
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

        // Any unclosed shake tags
        while (state.openEffects.Count > 0)
        {
            OpenEffect dangling = state.openEffects.Pop();
            Warn($"Unclosed <shake> tag starting at visible char {dangling.startVisibleCharacterIndex}." 
            + "Showing text without shake for unfinished span", state);
        }

        return new DialogueProcessedText(
            state.output.ToString(),
            state.spans,
            state.warnings
        );
    }

    private void HandleOpenTag(string tagName, ParserState state)
    {
        if (tagName != "shake")
        {
            Warn($"Unsupported custom tag <{tagName}>.", state);
            return;
        }

        foreach (OpenEffect open in state.openEffects)
        {
            if (open.effectType == DialogueTextEffectType.Shake)
            {
                Warn("Nested <shake> tags are not supported. Ignoring inner <shake>", state);
                return;
            }
        }

        state.openEffects.Push(new OpenEffect
        {
            effectType = DialogueTextEffectType.Shake,
            startVisibleCharacterIndex = state.visibleCharacterCount
        });
    }

    private void HandleCloseTag(string tagName, ParserState state)
    {
        if (tagName != "shake")
        {
            Warn($"Unsupported custom closing tag </{tagName}>.", state);
            return;
        }

        if (state.openEffects.Count == 0)
        {
            Warn("Found </shake> without a matching <shake>. Ignoring it.", state);
            return;
        }

        OpenEffect open = state.openEffects.Pop();

        if (open.effectType != DialogueTextEffectType.Shake)
        {
            Warn("Mismatched closing </shake> tag", state);
            return;
        }

        int length = state.visibleCharacterCount - open.startVisibleCharacterIndex;
        if (length <= 0)
        {
            Warn("Empty <shake></shake> span found. Ignoring it.", state);
            return;
        }

        state.spans.Add(new DialogueTextEffectSpan
        {
            effectType = DialogueTextEffectType.Shake,
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
    Shake
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

