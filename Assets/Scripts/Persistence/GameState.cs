using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class GameState : MonoBehaviour
{
    private static readonly Regex StableIdPattern = 
        new Regex("^[a-z][a-z0-9_]*$", RegexOptions.Compiled);

    private readonly HashSet<string> flags = 
        new HashSet<string>(StringComparer.Ordinal);

    public event Action StateChanged;

    public string CheckpointSceneName { get; private set; } = string.Empty;
    public string CheckpointSpawnId { get; private set; } = string.Empty;

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

        bool changed = false;

        foreach (string flag in flagsToSet)
        {
            if (!IsValidStableId(flag))
            {
                Debug.LogWarning(
                    $"GameState ignored invalid flag ID '{flag}'. " +
                    "Use lowercase_snake_case IDs.",
                    this
                );

                continue;
            }

            changed |= flags.Add(flag);
        }

        if (changed)
        {
            StateChanged?.Invoke();
        }
    }

    public bool RemoveFlag(string flag)
    {
        if (!IsValidStableId(flag))
        {
            Debug.LogWarning(
                $"GameState ignored invalid flag ID '{flag}'. " +
                "Use lowercase_snake_case IDs.",
                this
            );

            return false;
        }

        bool changed = flags.Remove(flag);

        if (changed)
        {
            StateChanged?.Invoke();
        }

        return changed;
    }

    public List<string> GetAllFlags()
    {
        List<string> allFlags = flags.ToList();
        allFlags.Sort(StringComparer.Ordinal);
        return allFlags;
    }

    public void SetCheckpoint(string sceneName, string spawnId)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning(
                "GameState ignored a checkpoint with no scene name.",
                this
            );

            return;
        }

        if (!IsValidStableId(spawnId))
        {
            Debug.LogWarning(
                $"GameState ignored invalid spawn ID '{spawnId}'. " +
                "Use lowercase_snake_case IDs.",
                this
            );

            return;
        }

        if (CheckpointSceneName == sceneName && 
            CheckpointSpawnId == spawnId)
        {
            return;
        }

        CheckpointSceneName = sceneName;
        CheckpointSpawnId = spawnId;

        StateChanged?.Invoke();
    }

    public void ResetState()
    {
        bool changed = 
            flags.Count > 0 ||
            !string.IsNullOrEmpty(CheckpointSceneName) ||
            !string.IsNullOrEmpty(CheckpointSpawnId);

        flags.Clear();
        CheckpointSceneName = string.Empty;
        CheckpointSpawnId = string.Empty;

        if (changed)
        {
            StateChanged?.Invoke();
        }
    }

    public void LoadState(
        IEnumerable<string> savedFlags,
        string checkpointSceneName,
        string checkpointSpawnId)
    {
        flags.Clear();

        if (savedFlags != null)
        {
            foreach (string flag in savedFlags)
            {
                if (IsValidStableId(flag))
                {
                    flags.Add(flag);
                }
                else
                {
                    Debug.LogWarning(
                        $"GameState skipped invalid saved flag ID '{flag}'.",
                        this
                    );
                }
            }
        }

        CheckpointSceneName = checkpointSceneName ?? string.Empty;

        CheckpointSpawnId = IsValidStableId(checkpointSpawnId)
            ? checkpointSpawnId
            : string.Empty;
    }

    public static bool IsValidStableId(string value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
                StableIdPattern.IsMatch(value);
    }
}
