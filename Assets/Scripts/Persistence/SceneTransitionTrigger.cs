using UnityEngine;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string destinationSceneName;
    [SerializeField] private string destinationSpawnId;

    public void BeginTransition()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError(
                "SceneTransitionTrigger could not find SceneManager.",
                this 
            );

            return;
        }

        SaveManager.Instance.TransitionToScene(
            destinationSceneName,
            destinationSpawnId
        );
    }

    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(destinationSpawnId) &&
            !GameState.IsValidStableId(destinationSpawnId))
        {
            Debug.LogWarning(
                $"Destination spawn ID '{destinationSpawnId}' should use " +
                "lowercase_snake_case.",
                this
            );
        }
    }
}
