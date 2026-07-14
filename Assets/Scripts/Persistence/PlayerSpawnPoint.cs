using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId;
    [SerializeField] private bool isDefaultForScene;

    public string SpawnId => spawnId;
    public bool IsDefaultForScene => isDefaultForScene;

    private void OnValidate()
    {
        if (!string.IsNullOrWhiteSpace(spawnId) &&
            !GameState.IsValidStableId(spawnId))
        {
            Debug.LogWarning(
                $"Spawn point '{name}' should use a lowercase_snake_case ID.",
                this
            );
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isDefaultForScene ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(
            transform.position,
            transform.position + transform.forward * 0.75f
        );
    }
}
