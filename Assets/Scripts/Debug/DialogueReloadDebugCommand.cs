using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueReloadDebugCommand : MonoBehaviour
{
    [SerializeField] private Key reloadKey = Key.F5;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Toggle visibility with F5
        if (Application.isPlaying && Keyboard.current[reloadKey].wasPressedThisFrame)
        {
            ReloadAllDialogue();
        }
    }

    private void ReloadAllDialogue()
    {
        DialogueYamlLoader[] yamlLoaders = FindObjectsByType<DialogueYamlLoader>();
        int count = 0;
        foreach(DialogueYamlLoader yamlLoader in yamlLoaders)
        {
            yamlLoader.Load();
            count++;
        }

        Debug.Log($"Reloaded {count} dialogue YAML files");
    }
}
