using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractionController : MonoBehaviour
{
    [Header("Input")]
    public InputActionAsset inputActions; // Reference to the input actions asset

    private InputAction interactAction;

    private float maxInteractionDistance = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize input actions
        if (inputActions == null)
        {
            Debug.LogError("Input Actions asset not assigned!");
            return;
        }

        // Get the Player action map
        var playerActionMap = inputActions.FindActionMap("Player");
        if (playerActionMap == null)
        {
            Debug.LogError("Player action map not found in Input Actions!");
            return;
        }

        // Get individual actions
        interactAction = playerActionMap.FindAction("Interact");

        if (interactAction == null)
        {
            Debug.LogError("Interact action not found");
            return;
        }

        // Subscribe to input events
        interactAction.started += OnInteract;

        // Enable the Player action map
        playerActionMap.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        // Find all NPCs by tag "NPC"
        GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
        if (npcs == null || npcs.Length == 0)
        {
            Debug.Log("No NPCs found with tag 'NPC'.");
            return;
        }

        // Find the nearest NPC
        GameObject nearest = null;
        float nearestDistSqr = float.MaxValue;
        Vector3 pos = transform.position;

        foreach (GameObject npc in npcs)
        {
            float d = (npc.transform.position - pos).sqrMagnitude;
            if (d < nearestDistSqr)
            {
                nearestDistSqr = d;
                nearest = npc;
            }
        }

        // Check if nearest is valid
        if (nearest != null && nearestDistSqr < Math.Pow(maxInteractionDistance, 2))
        {
            Debug.Log("Nearest NPC: " + nearest.name);
        }
    }
}
