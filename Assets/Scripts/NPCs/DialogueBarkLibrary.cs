using System.Collections.Generic;
using UnityEngine;

public class DialogueBarkLibrary : MonoBehaviour
{
    [SerializeField] private DialogueBarkProfile[] profiles;

    private Dictionary<string, DialogueBarkProfile> profilesByNpcId;

    public void Awake()
    {
        profilesByNpcId = new Dictionary<string, DialogueBarkProfile>();

        foreach(DialogueBarkProfile profile in profiles)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.NpcId))
            {
                continue;
            }

            if (profilesByNpcId.ContainsKey(profile.NpcId))
            {
                Debug.LogWarning(
                    $"Duplicate dialogue bark profile for NPC '{profile.NpcId}.'",
                    this
                );
            }

            profilesByNpcId.Add(profile.NpcId, profile);
        }
    }

    public DialogueBarkProfile GetProfile(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId) || profilesByNpcId == null)
        {
            return null;
        }

        profilesByNpcId.TryGetValue(npcId, out DialogueBarkProfile profile);
        return profile;
    }
}
