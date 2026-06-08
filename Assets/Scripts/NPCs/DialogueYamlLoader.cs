using UnityEngine;

public sealed class DialogueYamlLoader : MonoBehaviour
{
    [SerializeField] private TextAsset yamlFile;
    [SerializeField] private bool parseOnAwake = true;

    public NpcDialogueData DialogueData { get; private set; }

    private void Awake()
    {
        if (parseOnAwake)
        {
            Load();
        }
    }

    public void Load()
    {
        if (!DialogueYamlParser.TryParse(yamlFile, out NpcDialogueData data, out string error))
        {
            Debug.LogError($"Failed to parse dialogue YAML on '{name}': {error}", this);
            return;
        }

        DialogueData = data;
        Debug.Log(
            $"Loaded dialogue for NPC '{DialogueData.npcId}' with {DialogueData.conversations.Count} conversation(s).",
            this);
    }
}
