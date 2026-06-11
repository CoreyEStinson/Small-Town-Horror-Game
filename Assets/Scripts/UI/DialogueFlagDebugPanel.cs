using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueFlagDebugPanel : MonoBehaviour
{
    [SerializeField] private DialogueFlagStore dialogueFlagStore;
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TextMeshProUGUI flagText;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        debugPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Toggle visibility with F3
        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            bool newState = !debugPanel.activeSelf;
            debugPanel.SetActive(newState);
            if (newState) Refresh();
        }

        // Updates every frame. TODO: Make it update only whenever flags change
        Refresh();
    }

    public void Refresh()
    {
        List<string> flags = dialogueFlagStore.GetAllFlags();
        if (flags.Count == 0)
        {
            flagText.text = "(none)";
            return;
        }

        flagText.text = "";
        foreach (string flag in flags)
        {
            flagText.text += flag + "\n";
        }
    }
}
