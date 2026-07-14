using UnityEngine;
using UnityEngine.InputSystem;

public class SaveDebugInput : MonoBehaviour
{
    private void Update()
    {
        if (!Application.isPlaying ||
            Keyboard.current == null ||
            IrisTransitionController.IsTransitioning)
        {
            return;
        }

        if (Keyboard.current.f6Key.wasPressedThisFrame)
        {
            SaveManager.Instance?.SaveCurrentCheckpoint();
        }
        else if (Keyboard.current.f7Key.wasPressedThisFrame)
        {
            SaveManager.Instance?.LoadSavedGame();
        }
        else if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            SaveManager.Instance?.LogCurrentState();
        }
    }
}
