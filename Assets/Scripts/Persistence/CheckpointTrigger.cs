using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private PlayerSpawnPoint spawnPoint;

    private void Awake()
    {
        if (spawnPoint == null)
        {
            spawnPoint = GetComponent<PlayerSpawnPoint>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") ||
            spawnPoint == null ||
            SaveManager.Instance == null)
        {
            return;
        }

        SaveManager.Instance.ActivateCheckpoint(
            SceneManager.GetActiveScene().name,
            spawnPoint.SpawnId
        );
    }
}
