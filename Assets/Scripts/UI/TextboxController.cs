using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the display and interaction of dialogue textbox UI elements.
/// Handles speaker names, dialogue text, character portraits, and choice rendering.
/// </summary>
public sealed class TextboxController : MonoBehaviour
{
    [Header("Core UI")]
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Image portrait;
    [SerializeField] private DialogueTypewriter typewriter;
    [SerializeField] private GameObject textboxObject;

    [Header("Choice UI")]
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private Button choiceButtonPrefab;

    [Header("Portrait Loading")]
    [SerializeField] private string portraitResourcesFolder = "Portraits";
    [SerializeField] private Sprite fallbackPortrait;

    private readonly List<Button> spawnedChoiceButtons = new List<Button>();

    /// <summary>
    /// Invoked when the player selects a dialogue choice.
    /// </summary>
    public event Action<ChoiceData> ChoiceSelected;

    /// <summary>
    /// Gets whether the textbox UI is currently visible.
    /// </summary>
    public bool IsOpen => textboxObject.activeSelf;
    public bool IsTyping => typewriter != null && typewriter.IsTyping;

    /// <summary>
    /// Initializes the textbox by hiding it on scene load.
    /// </summary>
    private void Awake()
    {
        if (typewriter == null && bodyText != null)
        {
            typewriter = bodyText.GetComponent<DialogueTypewriter>();
            if (typewriter == null)
            {
                typewriter = bodyText.gameObject.AddComponent<DialogueTypewriter>();
            }
        }
        Hide();
    }

    /// <summary>
    /// Displays a dialogue line with NPC information, text, portrait, and choices.
    /// </summary>
    /// <param name="npcData">The NPC's dialogue data containing display name and portrait information.</param>
    /// <param name="line">The dialogue line data containing speaker override, body text, and choices.</param>
    /// <exception cref="ArgumentNullException">Thrown if npcData or line is null.</exception>
    public void ShowLine(NpcDialogueData npcData, LineData line)
    {
        if (npcData == null)
        {
            throw new ArgumentNullException(nameof(npcData));
        }

        if (line == null)
        {
            throw new ArgumentNullException(nameof(line));
        }

        textboxObject.SetActive(true);
        SetSpeakerText(ResolveSpeakerName(npcData, line));
        SetBodyText(line.bodyText);
        SetPortraitImage(ResolvePortrait(npcData, line));
        RenderChoices(line.choices);
    }

    /// <summary>
    /// Displays a simple message without choices.
    /// </summary>
    /// <param name="speakerName">The name of the speaker to display.</param>
    /// <param name="message">The message text to display.</param>
    /// <param name="speakerPortrait">Optional portrait sprite for the speaker. Uses fallback if null.</param>
    public void ShowMessage(string speakerName, string message, Sprite speakerPortrait = null)
    {
        textboxObject.SetActive(true);
        SetSpeakerText(speakerName);
        SetBodyText(message);
        SetPortraitImage(speakerPortrait);
        ClearChoices();
    }

    /// <summary>
    /// Hides the textbox UI and clears all displayed content.
    /// </summary>
    public void Hide()
    {
        ClearChoices();
        SetSpeakerText(string.Empty);
        ClearBodyText();
        SetPortraitImage(null);
        textboxObject.SetActive(false);
    }

    /// <summary>
    /// Sets the speaker name text in the UI.
    /// </summary>
    /// <param name="text">The speaker name to display. Empty string if null.</param>
    public void SetSpeakerText(string text)
    {
        if (speakerText != null)
        {
            speakerText.text = text ?? string.Empty;
        }
    }

    /// <summary>
    /// Sets the dialogue body text in the UI.
    /// </summary>
    /// <param name="text">The dialogue text to display. Empty string if null.</param>
    public void SetBodyText(string text)
    {
        if (typewriter != null)
        {
            typewriter.Play(text);
            return;
        }

        if (bodyText != null)
        {
            bodyText.text = text ?? string.Empty;
        }
    }

    public void CompleteTyping()
    {
        typewriter?.Complete();
    }

    /// <summary>
    /// Sets the speaker portrait image in the UI.
    /// </summary>
    /// <param name="sprite">The portrait sprite to display. Uses fallback if null.</param>
    public void SetPortraitImage(Sprite sprite)
    {
        if (portrait == null)
        {
            return;
        }

        portrait.sprite = sprite != null ? sprite : fallbackPortrait;
        portrait.enabled = portrait.sprite != null;
        portrait.preserveAspect = true;
    }

    /// <summary>
    /// Resolves the speaker name from line override or NPC data.
    /// </summary>
    /// <param name="npcData">The NPC dialogue data containing the default display name.</param>
    /// <param name="line">The dialogue line potentially containing a speaker name override.</param>
    /// <returns>The resolved speaker name to display.</returns>
    private string ResolveSpeakerName(NpcDialogueData npcData, LineData line)
    {
        if (!string.IsNullOrWhiteSpace(line.speakerOverride))
        {
            return line.speakerOverride;
        }

        return npcData.displayName;
    }

    /// <summary>
    /// Resolves the portrait sprite from NPC data and loads it from resources.
    /// </summary>
    /// <param name="npcData">The NPC dialogue data containing portrait resource path information.</param>
    /// <returns>The loaded portrait sprite, or the fallback sprite if loading fails.</returns>
    private Sprite ResolvePortrait(NpcDialogueData npcData)
    {
        return ResolvePortrait(npcData, null);
    }

    private Sprite ResolvePortrait(NpcDialogueData npcData, LineData line)
    {
        if (!string.IsNullOrWhiteSpace(line?.portrait))
        {
            Sprite linePortrait = LoadPortraitByName(line.portrait);
            if (linePortrait != null)
            {
                return linePortrait;
            }
        }

        if (npcData == null)
        {
            return fallbackPortrait;
        }

        if (string.IsNullOrWhiteSpace(npcData.portrait))
        {
            return fallbackPortrait;
        }

        Sprite loadedPortrait = LoadPortraitByName(npcData.portrait);
        return loadedPortrait != null ? loadedPortrait : fallbackPortrait;
    }

    private Sprite LoadPortraitByName(string portraitName)
    {
        if (string.IsNullOrWhiteSpace(portraitName))
        {
            return null;
        }

        string resourcePath = string.IsNullOrWhiteSpace(portraitResourcesFolder)
            ? portraitName
            : $"{portraitResourcesFolder}/{portraitName}";

        return Resources.Load<Sprite>(resourcePath);
    }

    /// <summary>
    /// Renders dialogue choice buttons in the UI.
    /// </summary>
    /// <param name="choices">The list of dialogue choices to render. If null or empty, no choices are displayed.</param>
    private void RenderChoices(List<ChoiceData> choices)
    {
        ClearChoices();

        if (choices == null || choices.Count == 0)
        {
            return;
        }

        if (choicesContainer == null || choiceButtonPrefab == null)
        {
            Debug.LogWarning(
                "TextboxController received dialogue choices but choice UI is not assigned.",
                this);
            return;
        }

        foreach (ChoiceData choice in choices)
        {
            if (choice == null)
            {
                continue;
            }

            Button buttonInstance = Instantiate(choiceButtonPrefab, choicesContainer);
            spawnedChoiceButtons.Add(buttonInstance);

            TextMeshProUGUI buttonLabel = buttonInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonLabel != null)
            {
                buttonLabel.text = choice.choiceText;
            }

            buttonInstance.onClick.AddListener(() => ChoiceSelected?.Invoke(choice));
        }
    }

    /// <summary>
    /// Removes all spawned choice buttons and clears their listeners.
    /// </summary>
    private void ClearChoices()
    {
        foreach (Button button in spawnedChoiceButtons)
        {
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveAllListeners();
            Destroy(button.gameObject);
        }

        spawnedChoiceButtons.Clear();
    }

    private void ClearBodyText()
    {
        if (typewriter != null)
        {
            typewriter.StopAndClear();
            return;
        }

        if (bodyText != null)
        {
            bodyText.text = string.Empty;
        }
    }
}
