using System.Collections.Generic;
using UnityEngine;

public sealed class DialogueFlagStore : MonoBehaviour
{
    private readonly HashSet<string> flags = new HashSet<string>();

    public bool HasFlag(string flag)
    {
        return !string.IsNullOrWhiteSpace(flag) && flags.Contains(flag);
    }

    public bool MeetsAll(IReadOnlyList<string> requiredFlags)
    {
        if (requiredFlags == null || requiredFlags.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < requiredFlags.Count; i++)
        {
            if (!HasFlag(requiredFlags[i]))
            {
                return false;
            }
        }

        return true;
    }

    public void SetFlags(IEnumerable<string> flagsToSet)
    {
        if (flagsToSet == null)
        {
            return;
        }

        foreach (string flag in flagsToSet)
        {
            if (!string.IsNullOrWhiteSpace(flag))
            {
                flags.Add(flag);
            }
        }
    }
}
