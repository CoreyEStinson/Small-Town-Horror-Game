using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueFlagDebugPanel : MonoBehaviour
{
    [SerializeField] private GameState gameState;
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TextMeshProUGUI flagText;

    private void Awake()
    {
        if (gameState == null)
        {
            gameState = FindAnyObjectByType<GameState>();
        }
    }

    private void OnEnable()
    {
        if (gameState != null)
        {
            gameState.StateChanged += Refresh;
        }
    }

    private void Start()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(false);
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (gameState != null)
        {
            gameState.StateChanged -= Refresh;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null ||
            !Keyboard.current.f3Key.wasPressedThisFrame ||
            debugPanel == null)
        {
            return;
        }

        bool shouldShow = !debugPanel.activeSelf;
        debugPanel.SetActive(shouldShow);

        if (shouldShow)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (flagText == null)
        {
            return;
        }

        if (gameState == null)
        {
            flagText.text = "(GameState missing)";
            return;
        }

        List<string> flags = gameState.GetAllFlags();

        flagText.text = flags.Count == 0
            ? "(none)"
            : string.Join("\n", flags);
    }
}