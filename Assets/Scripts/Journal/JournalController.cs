using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class JournalController : MonoBehaviour
{
    public static JournalController Instance { get; private set; }

    private enum JournalTab
    {
        Active,
        Completed,
        Notes
    }

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string playerActionMapName = "Player";
    [SerializeField] private string openJournalActionName = "OpenJournal";

    [Header("Journal Root")]
    [SerializeField] private GameObject journalRoot;

    [Header("Tabs")]
    [SerializeField] private Button activeTabButton;
    [SerializeField] private Button completedTabButton;
    [SerializeField] private Button notesTabButton;
    [SerializeField] private Button closeButton;

    [Header("Background")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private List<Sprite> backgroundSprites;

    [Header("Pages")]
    [SerializeField] private GameObject questPage;
    [SerializeField] private GameObject notesPage;

    [Header("Quest List")]
    [SerializeField] private Transform questListContent;
    [SerializeField] private Button questListEntryPrefab;

    [Header("Quest Details")]
    [SerializeField] private TMP_Text questTitleText;
    [SerializeField] private TMP_Text questStatusText;
    [SerializeField] private TMP_Text questSummaryText;
    [SerializeField] private TMP_Text questLocationText;
    [SerializeField] private TMP_Text questGiverText;
    [SerializeField] private TMP_Text currentObjectiveText;
    [SerializeField] private TMP_Text objectiveChecklistText;
    [SerializeField] private Button trackQuestButton;
    [SerializeField] private TMP_Text trackQuestButtonText;

    [Header("Note Spread")]
    [SerializeField] private TMP_Text leftNoteTitleText;
    [SerializeField] private TMP_Text leftNoteBodyText;
    [SerializeField] private Image leftNoteImage;

    [SerializeField] private TMP_Text rightNoteTitleText;
    [SerializeField] private TMP_Text rightNoteBodyText;
    [SerializeField] private Image rightNoteImage;

    [SerializeField] private Button previousSpreadButton;
    [SerializeField] private Button nextSpreadButton;

    [Header("Optional")]
    [SerializeField] private DialogueRunner dialogueRunner;

    public bool IsOpen => journalRoot != null && journalRoot.activeSelf;
    
    private readonly List<Button> spawnedListEntries = new List<Button>();

    private JournalManager journalManager;
    private InputAction openJournalAction;
    private JournalTab currentTab = JournalTab.Active;
    private string selectedQuestId;
    private int noteSpreadStartIndex;
    private bool isRefreshing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        if (journalRoot != null)
        {
            journalRoot.SetActive(false);
        }
    }

    private void Start()
    {
        journalManager = JournalManager.Instance;

        if (journalManager == null)
        {
            Debug.LogError(
                "JournalController could not find a JournalManager.",
                this
            );
        }
        else
        {
            journalManager.JournalChanged += Refresh;
            journalManager.NoteDiscovered += OpenDiscoveredNote;
        }

        SetupInput();
        SetupButtons();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (journalManager != null)
        {
            journalManager.JournalChanged -= Refresh;
            journalManager.NoteDiscovered -= OpenDiscoveredNote;
        }

        if (openJournalAction != null)
        {
            openJournalAction.started -= OnOpenJournalPressed;
        }
    }

    private void SetupInput()
    {
        if (inputActions == null)
        {
            Debug.LogError(
                "JournalController needs the Input Actions asset assigned.",
                this
            );
            return;
        }

        InputActionMap playerMap = 
            inputActions.FindActionMap(playerActionMapName);

        if (playerMap == null)
        {
            Debug.LogError(
                $"Could not find action map '{playerActionMapName}'.",
                this
            );
            return;
        }

        openJournalAction = playerMap.FindAction(openJournalActionName);

        if (openJournalAction == null)
        {
            Debug.LogError(
                $"Could not find action '{openJournalActionName}'.",
                this
            );
            return;
        }
        
        playerMap.Enable();
        openJournalAction.started += OnOpenJournalPressed;
    }

    private void SetupButtons()
    {
        activeTabButton?.onClick.AddListener(ShowActiveTab);
        completedTabButton?.onClick.AddListener(ShowCompletedTab);
        notesTabButton?.onClick.AddListener(ShowNotesTab);
        closeButton?.onClick.AddListener(Close);

        trackQuestButton?.onClick.AddListener(TrackSelectedQuest);

        previousSpreadButton?.onClick.AddListener(ShowPreviousSpread);
        nextSpreadButton?.onClick.AddListener(ShowNextSpread);
    }

    private void OnOpenJournalPressed(InputAction.CallbackContext context)
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    public void Open()
    {
        if (journalManager == null || !journalManager.IsInitialized)
        {
            return;
        }

        if (FadeTransition.IsTransitioning ||
            (dialogueRunner != null && dialogueRunner.IsDialogueOpen))
        {
            return;
        }

        journalRoot.SetActive(true);

        if (string.IsNullOrWhiteSpace(selectedQuestId))
        {
            selectedQuestId = journalManager.Data.trackedQuestId;
        }

        ShowActiveTab();
    }

    public void Close()
    {
        if (journalRoot != null)
        {
            journalRoot.SetActive(false);
        }
    }

    public void ShowActiveTab()
    {
        currentTab = JournalTab.Active;
        SetBackground(0);

        questPage?.SetActive(true);
        notesPage?.SetActive(false);

        EnsureSelectedActiveQuest();
        Refresh();
    }

    public void ShowCompletedTab()
    {
        currentTab = JournalTab.Completed;
        SetBackground(1);

        questPage?.SetActive(true);
        notesPage?.SetActive(false);

        EnsureSelectedActiveQuest();
        Refresh();
    }

    public void ShowNotesTab()
    {
        currentTab = JournalTab.Notes;
        SetBackground(2);
        selectedQuestId = string.Empty;

        questPage?.SetActive(false);
        notesPage?.SetActive(true);

        EnsureNoteSpreadIsValid();
        Refresh();
    }

    public void Refresh()
    {
        if (isRefreshing || 
            !IsOpen || 
            journalManager == null || 
            !journalManager.IsInitialized)
        {
            return;
        }

        isRefreshing = true;
        
        try
        {
            switch (currentTab)
            {
                case JournalTab.Active:
                    RebuildQuestList(false);
                    ShowSelectedQuest();
                    break;
                
                case JournalTab.Completed:
                    RebuildQuestList(true);
                    ShowSelectedQuest();
                    break;

                case JournalTab.Notes:
                    ShowCurrentNoteSpread();
                    break;
            }
        }
        finally
        {
            isRefreshing = false;
        } 
    }

    private void RebuildQuestList(bool showCompleted)
    {
        ClearListEntries();

        List<QuestSaveData> quests = GetSortedQuestStates(showCompleted);

        foreach (QuestSaveData questState in quests)
        {
            if (!journalManager.TryGetQuestDefinition(
                questState.questId,
                out Quest quest
            ))
            {
                continue;
            }

            string prefix = quest.type == Quest.QuestType.Main
                ? "[Main] "
                : "[Side] ";

            string selectedMarker = quest.id == selectedQuestId
                ? "> "
                : "<color=#00000000>> </color>";

            CreateListEntry(
                $"{selectedMarker}{prefix}{quest.title}",
                () => SelectQuest(quest.id)
            );
        }

        if (quests.Count == 0)
        {
            CreateListEntry(
                showCompleted
                    ? "No completed quests."
                    : "No active quests.",
                null
            );
        }
    }    

    private void CreateListEntry(string label, Action OnClick)
    {
        if (questListEntryPrefab == null || questListContent == null)
        {
            return;
        }

        Button entry = Instantiate(
            questListEntryPrefab,
            questListContent
        );

        TMP_Text entryText = entry.GetComponentInChildren<TMP_Text>();

        if (entryText != null)
        {
            entryText.text = label;
        }

        if (OnClick == null)
        {
            entry.interactable = false;
        }
        else
        {
            entry.onClick.AddListener(() => OnClick());
        }

        spawnedListEntries.Add(entry);
    }

    private void ClearListEntries()
    {
        foreach (Button entry in spawnedListEntries)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }
        }

        spawnedListEntries.Clear();
    }

    private void ClearNotePage(
        TMP_Text titleText,
        TMP_Text bodyText,
        Image image
    )
    {
        titleText.text = string.Empty;
        bodyText.text = string.Empty;

        image.sprite = null;
        image.gameObject.SetActive(false);
    }

    private void SelectQuest(string questId)
    {
        selectedQuestId = questId;
        ShowSelectedQuest();
        RebuildQuestList(currentTab == JournalTab.Completed);
    }

    private void ShowSelectedQuest()
    {
        if (!journalManager.TryGetQuestDefinition(
            selectedQuestId,
            out Quest quest
        ))
        {
            SetQuestDetailsEmpty();
            return;
        }

        QuestSaveData questState = FindQuestState(selectedQuestId);

        if (questState == null)
        {
            SetQuestDetailsEmpty();
            return;
        }

        Objective currentObjective = 
            journalManager.GetCurrentObjective(selectedQuestId);

        questTitleText.text = quest.title;
        questStatusText.text = questState.isCompleted
            ? "Completed"
            : "Active";
        
        questSummaryText.text = questState.isCompleted
            ? quest.completionSummary
            : quest.summary;

        currentObjectiveText.text = currentObjective != null
            ? FormatObjective(currentObjective, questState)
            : (questState.isCompleted
                ? "Quest completed."
                : "No active objective");
        
        questLocationText.text = 
            currentObjective != null &&
            !string.IsNullOrWhiteSpace(currentObjective.location)
                ? $"Location: {currentObjective.location}"
                : string.Empty;
        
        questGiverText.text =
            currentObjective != null &&
            !string.IsNullOrWhiteSpace(currentObjective.giver)
                ? $"Given by: {currentObjective.giver}"
                : string.Empty;

        objectiveChecklistText.text = 
            BuildObjectiveChecklist(quest, questState);

        bool canTrack = !questState.isCompleted;
        trackQuestButton.gameObject.SetActive(canTrack);

        if (canTrack)
        {
            bool isTracked = 
                journalManager.Data.trackedQuestId == quest.id;

            trackQuestButton.interactable = !isTracked;
            trackQuestButtonText.text = isTracked
                ? "Tracked"
                : "Track Quest";
        }
    }

    private void ShowCurrentNoteSpread()
    {
        List<NoteSaveData> notes = GetNotesInDiscoveryOrder();

        if (notes.Count == 0)
        {
            ClearNotePage(
                leftNoteTitleText,
                leftNoteBodyText,
                leftNoteImage
            );

            ClearNotePage(
                rightNoteTitleText,
                rightNoteBodyText,
                rightNoteImage
            );

            previousSpreadButton.gameObject.SetActive(false);
            nextSpreadButton.gameObject.SetActive(false);
            return;
        }

        EnsureNoteSpreadIsValid();

        ShowNoteOnPage(
            notes[noteSpreadStartIndex],
            leftNoteTitleText,
            leftNoteBodyText,
            leftNoteImage
        );

        int rightNoteIndex = noteSpreadStartIndex + 1;

        if (rightNoteIndex < notes.Count)
        {
            ShowNoteOnPage(
                notes[rightNoteIndex],
                rightNoteTitleText,
                rightNoteBodyText,
                rightNoteImage
            );
        }
        else
        {
            ClearNotePage(
                rightNoteTitleText,
                rightNoteBodyText,
                rightNoteImage
            );
        }

        previousSpreadButton.gameObject.SetActive(
            noteSpreadStartIndex > 0
        );

        nextSpreadButton.gameObject.SetActive(
            noteSpreadStartIndex + 2 < notes.Count
        );
    }

    private void ShowNoteOnPage(
        NoteSaveData noteState,
        TMP_Text titleText,
        TMP_Text bodyText,
        Image image
    )
    {
        if (!journalManager.TryGetNoteDefinition(
            noteState.noteId,
            out Note note
        ))
        {
            ClearNotePage(titleText, bodyText, image);
            return;
        }

        titleText.text = note.title;
        bodyText.text = note.bodyText;

        Sprite noteSprite = LoadNoteImage(note.imagePath);

        image.sprite = noteSprite;
        image.gameObject.SetActive(noteSprite != null);

        journalManager.MarkNoteRead(note.id);
    }

    private void ShowPreviousSpread()
    {
        noteSpreadStartIndex -= 2;
        EnsureNoteSpreadIsValid();
        Refresh();
    }

    private void ShowNextSpread()
    {
        noteSpreadStartIndex += 2;
        EnsureNoteSpreadIsValid();
        Refresh();
    }

    // Uses the background sprite that matches the currently open journal tab.
    private void SetBackground(int spriteIndex)
    {
        if (backgroundImage == null)
        {
            return;
        }

        if (backgroundSprites == null ||
            spriteIndex < 0 ||
            spriteIndex >= backgroundSprites.Count)
        {
            Debug.LogWarning(
                $"Journal background sprite index {spriteIndex} is not assigned.",
                this
            );
            return;
        }

        backgroundImage.sprite = backgroundSprites[spriteIndex];
    }
    private void SetQuestDetailsEmpty()
    {
        questTitleText.text = "No quest selected";
        questStatusText.text = string.Empty;
        questSummaryText.text = string.Empty;
        questLocationText.text = string.Empty;
        questGiverText.text = string.Empty;
        currentObjectiveText.text = string.Empty;
        objectiveChecklistText.text = string.Empty;
        trackQuestButton.gameObject.SetActive(false);
    }

    private string BuildObjectiveChecklist(Quest quest, QuestSaveData questState)
    {
        List<string> lines = new List<string>();

        foreach (Objective objective in quest.objectives)
        {
            ObjectiveSaveData objectiveState = FindObjectiveState(questState, objective.id);

            if (objectiveState == null ||
                (!objectiveState.isActive &&
                 !objectiveState.isCompleted))
            {
                lines.Add("???");
                continue;
            }

            string prefix = objectiveState.isCompleted ? "[x]" : "[ ]";
            lines.Add(
                $"{prefix} {FormatObjective(objective, questState)}"
            );
        }

        return string.Join("\n", lines);
    }

    private string FormatObjective(Objective objective, QuestSaveData questState)
    {
        if (!objective.progressTarget.HasValue)
        {
            return objective.text;
        }

        ObjectiveSaveData objectiveState = 
            FindObjectiveState(questState, objective.id);

        int progress = objectiveState != null
            ? objectiveState.currentProgress
            : 0;

        return $"{objective.text} ({progress}/{objective.progressTarget.Value})";
    }

    private void TrackSelectedQuest()
    {
        if (!string.IsNullOrWhiteSpace(selectedQuestId))
        {
            journalManager.SetTrackedQuest(selectedQuestId);
        }
    }

    private void OpenDiscoveredNote(Note note)
    {
        if (note == null)
        {
            return;
        }

        journalRoot.SetActive(true);
        currentTab = JournalTab.Notes;
        SetBackground(2);

        questPage?.SetActive(false);
        notesPage?.SetActive(true);

        List<NoteSaveData> notes = GetNotesInDiscoveryOrder();

        int discoveredNoteIndex = notes.FindIndex(
            noteState => noteState.noteId == note.id
        );

        if (discoveredNoteIndex >= 0)
        {
            noteSpreadStartIndex = (discoveredNoteIndex / 2) * 2;
        }

        Refresh();
    }

    private Sprite LoadNoteImage(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }   
        
        return Resources.Load<Sprite>(imagePath);
    }

    private List<QuestSaveData> GetSortedQuestStates(bool completed)
    {
        return journalManager.Data.quests
            .Where(questState => questState.isCompleted == completed)
            .Where(questState =>
                journalManager.TryGetQuestDefinition(
                    questState.questId,
                    out _))
            .OrderBy(questState =>
            {
                journalManager.TryGetQuestDefinition(
                    questState.questId,
                    out Quest quest
                );

                return quest.type == Quest.QuestType.Main ? 0 : 1;
            })
            .ThenByDescending(questState => questState.acquiredOrder)
            .ToList();
    }

    private List<NoteSaveData> GetNotesInDiscoveryOrder()
    {
        return journalManager.Data.notes
            .OrderBy(noteState => noteState.discoveryOrder)
            .ToList();
    }

    private void EnsureSelectedActiveQuest()
    {
        QuestSaveData currentSelection = 
            FindQuestState(selectedQuestId);
        
        if (currentSelection != null && 
            !currentSelection.isCompleted)
        {
            return;
        }

        selectedQuestId = journalManager.Data.trackedQuestId;

        if (FindQuestState(selectedQuestId) != null)
        {
            return;
        }

        List<QuestSaveData> activeQuests =
            GetSortedQuestStates(false);

        selectedQuestId = activeQuests.Count > 0
            ? activeQuests[0].questId
            : string.Empty;
    }

    private void EnsureSelectedCompletedQuest()
    {
        QuestSaveData currentSelection = 
            FindQuestState(selectedQuestId);

        if (currentSelection != null &&
            currentSelection.isCompleted)
        {
            return;
        }

        List<QuestSaveData> completedQuests =
            GetSortedQuestStates(true);

        selectedQuestId = completedQuests.Count > 0
            ? completedQuests[0].questId
            : string.Empty;
    }

    private void EnsureNoteSpreadIsValid()
    {
        List<NoteSaveData> notes = GetNotesInDiscoveryOrder();

        if (notes.Count == 0)
        {
            noteSpreadStartIndex = 0;
            return;
        } 

        int finalSpreadStartIndex = ((notes.Count - 1) / 2) * 2;

        noteSpreadStartIndex = Mathf.Clamp(
            noteSpreadStartIndex,
            0,
            finalSpreadStartIndex
        );
    }

    private QuestSaveData FindQuestState(string questId)
    {
        return journalManager.Data.quests.FirstOrDefault(
            questState => questState.questId == questId
        );
    }

    private ObjectiveSaveData FindObjectiveState(
        QuestSaveData questState,
        string objectiveId
    )
    {
        if (questState == null)
        {
            return null;
        }

        return questState.objectives.FirstOrDefault(
            objectiveState => objectiveState.objectiveId == objectiveId
        );
    }
}




