using System.Collections.Generic;
using UnityEngine;

public class JournalYamlLoader : MonoBehaviour
{
    [SerializeField] private TextAsset[] yamlFiles;
    [SerializeField] private bool parseOnAwake = true;

    public JournalData JournalData { get; private set; }

    private void Awake()
    {
        if (parseOnAwake)
        {
            Load();
        }
    }

    public void Load()
    {
        if (yamlFiles != null && yamlFiles.Length != 0)
        {
            foreach (TextAsset file in yamlFiles)
            {
                if (!JournalYamlParser.TryParse(file, out JournalData data, out string error))
                {
                    Debug.LogError($"Failed to parse journal yaml: {error}", this);
                    continue;
                }

                Merge(JournalData, data);
            }

            Debug.Log(
                $"Loaded journal with {JournalData?.quests?.Count} quests and {JournalData?.notes?.Count} notes");
        }
    }

    public void Merge(JournalData all, JournalData addition)
    {
        if (all == null)
        {
            JournalData = addition;
            return;
        }

        if (addition == null)
        {
            Debug.LogError("Cannot merge: addition JournalData is null", this);
            return;
        }

        if (all.quests == null)
        {
            all.quests = addition.quests != null ? new List<Quest>(addition.quests) : new List<Quest>();
        }
        else if (addition.quests != null)
        {
            all.quests.AddRange(addition.quests);
        }

        if (all.notes == null)
        {
            all.notes = addition.notes != null ? new List<Note>(addition.notes) : new List<Note>();
        }
        else if (addition.notes != null)
        {
            all.notes.AddRange(addition.notes);
        }
    }
}