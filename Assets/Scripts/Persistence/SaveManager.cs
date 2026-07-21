using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public const int CurrentSaveVersion = 2;

    public static SaveManager Instance { get; private set; }

    private GameState gameState;
    private GameSession gameSession;
    private FadeTransition fadeTransition;
    private bool startupStarted;

    private string SavePath => 
        Path.Combine(Application.persistentDataPath, "savegame.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        gameState = GetComponent<GameState>();
        gameSession = GetComponent<GameSession>();
        fadeTransition = GetComponent<FadeTransition>();
    }

    private void Start()
    {
        BeginStartup();
    }

    public void BeginStartup()
    {
        if (startupStarted)
        {
            return;
        }

        startupStarted = true;
        StartCoroutine(BeginStartupRoutine());
    }

    public void ActivateCheckpoint(string sceneName, string spawnId)
    {
        if (gameState == null)
        {
            Debug.LogError("SaveManager cannot find GameState.", this);
            return;
        }

        gameState.SetCheckpoint(sceneName, spawnId);
        SaveCurrentCheckpoint();
    }

    public bool SaveCurrentCheckpoint()
    {
        if (gameState == null)
        {
            Debug.LogError("SaveManager cannot find GameState.", this);
            return false;
        }

        if (string.IsNullOrWhiteSpace(gameState.CheckpointSceneName) ||
            string.IsNullOrWhiteSpace(gameState.CheckpointSceneName))
        {
            Debug.LogWarning(
                "Save skipped because no active checkpoint exists.",
                this
            );

            return false;
        }

        SaveGameData data = new SaveGameData
        {
            saveVersion = CurrentSaveVersion,
            activeFlags = gameState.GetAllFlags(),
            checkpointSceneName = gameState.CheckpointSceneName,
            checkpointSpawnId = gameState.CheckpointSpawnId,
            journal = gameSession.JournalSaveData
        };

        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);

            string temporaryPath = SavePath + ".tmp";

            File.WriteAllText(
                temporaryPath,
                JsonUtility.ToJson(data, true)
            );

            File.Copy(temporaryPath, SavePath, true);
            File.Delete(temporaryPath);

            Debug.Log(
                $"Saved game at checkpoint " + 
                $"'{data.checkpointSceneName}/{data.checkpointSpawnId}'.",
                this
            );

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"Failed to save game to '{SavePath}': " + 
                e.Message,
                this
            );

            return false;
        }
    }

    public void DeleteSaveAndRestart()
    {
        if (FadeTransition.IsTransitioning)
        {
            return;
        }

        DeleteSaveFile();
        StartCoroutine(StartFreshGameRoutine());
    }

    public bool DeleteSaveFile()
    {
        if (FadeTransition.IsTransitioning)
        {
            return false;
        }

        try
        {
            bool deletedAnyFile = false;

            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                deletedAnyFile = true;
            }

            string temporaryPath = SavePath + ".tmp";

            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
                deletedAnyFile = true;
            }

            Debug.Log(
                deletedAnyFile
                    ? "Deleted save file."
                    : "No save file existed to delete",
                this
            );

            return deletedAnyFile;
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"Failed to delete save file: {e.Message}",
                this
            );

            return false;
        }
        
    }

    public void LoadSavedGame()
    {
        if (FadeTransition.IsTransitioning)
        {
            return;
        }

        if (!TryReadSave(out SaveGameData data, out string error))
        {
            Debug.LogWarning(
                $"Could not load save: {error}. Starting fresh.",
                this
            );

            StartCoroutine(StartFreshGameRoutine());
            return;
        }

        StartCoroutine(LoadSaveRoutine(data));
    }

    public void TransitionToScene(
        string destinationSceneName,
        string destinationSpawnId)
    {
        if (FadeTransition.IsTransitioning)
        {
            return;
        }

        if (!CanLoadScene(destinationSceneName))
        {
            Debug.LogError(
                $"Cannot transition because scene " +
                $"'{destinationSceneName}' is not in Build Settings",
                this
            );

            return;
        }

        if (!GameState.IsValidStableId(destinationSpawnId))
        {
            Debug.LogError(
                $"Cannot transition because '{destinationSpawnId}' " + 
                "is not a valid lowercase_snake_case spawn ID.",
                this
            );

            return;
        }

        SaveCurrentCheckpoint();

        StartCoroutine(
            TransitionRoutine(
                destinationSceneName,
                destinationSpawnId,
                fallBackToDefaultSpawn: false
            )
        );
    }

    public void LogCurrentState()
    {
        if (gameState == null)
        {
            Debug.LogWarning("SaveManager cannot find GameState.", this);
            return;
        }

        string flags = string.Join(", ", gameState.GetAllFlags());

        Debug.Log(
            $"Save state | Checkpoint: " + 
            $"{gameState.CheckpointSceneName}/" + 
            $"{gameState.CheckpointSpawnId} | " + 
            $"Flags: {(string.IsNullOrWhiteSpace(flags) ? "(none)" : flags)}",
            this
        );
    }

    private IEnumerator BeginStartupRoutine()
    {
        fadeTransition?.EnsureClosed();

        yield return null;

        if (TryReadSave(out SaveGameData data, out string error))
        {
            yield return LoadSaveRoutine(data);
            yield break;
        }

        if (error != "No save file exisits")
        {
            Debug.LogWarning(
                $"Could not load save data: {error}. Sarting fresh.",
                this
            );
        }

        yield return StartFreshGameRoutine();
    }

    private IEnumerator StartFreshGameRoutine()
    {
        if (fadeTransition != null)
        {
            yield return fadeTransition.Close();
        }

        gameState?.ResetState();
        PlaceAtDefaultSpawn(setCheckpoint: true);

        if (fadeTransition != null)
        {
            yield return fadeTransition.Open();
        }

        gameSession.JournalManager.StartNewGame();
    }

    private IEnumerator LoadSaveRoutine(SaveGameData data)
    {
        gameState?.LoadState(
            data.activeFlags,
            data.checkpointSceneName,
            data.checkpointSpawnId
        );

        gameSession.SetJournalSaveData(data.journal);

        if (!CanLoadScene(data.checkpointSceneName))
        {
            Debug.LogWarning(
                $"Saved scene '{data.checkpointSceneName}' is unavaliable. " +
                "Starting fresh",
                this
            );

            yield return StartFreshGameRoutine();
            yield break;
        }

        yield return TransitionRoutine(
            data.checkpointSceneName,
            data.checkpointSpawnId,
            fallBackToDefaultSpawn: true
        );
    }

    private IEnumerator TransitionRoutine(
        string sceneName,
        string spawnId,
        bool fallBackToDefaultSpawn)
    {
        if (fadeTransition != null)
        {
            yield return fadeTransition.Close();
        }

        if (SceneManager.GetActiveScene().name != sceneName)
        {
            AsyncOperation loadOperation = 
                SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            
            while (!loadOperation.isDone)
            {
                yield return null;
            }

            yield return WaitForSceneReady();
        }

        bool placedAtRequestedSpawn = PlaceAtSpawn(spawnId);

        if (!placedAtRequestedSpawn && fallBackToDefaultSpawn)
        {
            PlaceAtDefaultSpawn(setCheckpoint: true);
        }
        else if(placedAtRequestedSpawn)
        {
            gameState.SetCheckpoint(
                SceneManager.GetActiveScene().name,
                spawnId
            );
        }

        if (fadeTransition != null)
        {
            yield return fadeTransition.Open();
        }
    }

    private IEnumerator WaitForSceneReady()
    {
        while (!SceneManager.GetActiveScene().isLoaded ||
               GameObject.FindGameObjectWithTag("Player") == null ||
               FindSceneSpawns().Length == 0)
        {
            yield return null;
        }

        yield return new WaitForEndOfFrame();
    }
     private bool PlaceAtSpawn(string spawnId)
    {
        if (!GameState.IsValidStableId(spawnId))
        {
            Debug.LogWarning(
                $"Saved spawn ID '{spawnId}' is invalid",
                this
            );

            return false;
        }

        PlayerSpawnPoint[] matchingSpawns = FindSceneSpawns()
            .Where(spawn => spawn.SpawnId == spawnId)
            .ToArray();

        if (matchingSpawns.Length == 0)
        {
            Debug.LogError(
                $"No spawn point with ID '{spawnId}' exists in scene " +
                $"'{SceneManager.GetActiveScene().name}'.",
                this
            );

            return false;
        }

        if (matchingSpawns.Length > 1)
        {
            Debug.LogError(
                $"Scene '{SceneManager.GetActiveScene().name}' contains " +
                $"duplicate spawn ID '{spawnId}'.",
                this
            );

            return false;
        }

        PlacePlayer(matchingSpawns[0]);
        return true;
    }

    private void PlaceAtDefaultSpawn(bool setCheckpoint)
    {
        PlayerSpawnPoint[] defaultSpawns = FindSceneSpawns()
            .Where(spawn => spawn.IsDefaultForScene)
            .ToArray();

        if (defaultSpawns.Length == 0)
        {
            Debug.LogError(
                $"Scene '{SceneManager.GetActiveScene().name}' has no " +
                "default PlayerSpawnPoint.",
                this
            );

            return;
        }

        if (defaultSpawns.Length > 1)
        {
            Debug.LogError(
                $"Scene '{SceneManager.GetActiveScene().name}' has " +
                "multiple default PlayerSpawnPoints.",
                this
            );

            return;
        }

        PlayerSpawnPoint defaultSpawn = defaultSpawns[0];

        PlacePlayer(defaultSpawn);

        if (setCheckpoint)
        {
            gameState?.SetCheckpoint(
                SceneManager.GetActiveScene().name,
                defaultSpawn.SpawnId
            );
        }
    }

    private PlayerSpawnPoint[] FindSceneSpawns()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        return FindObjectsByType<PlayerSpawnPoint>()
        .Where(spawn => spawn.gameObject.scene == activeScene)
        .ToArray();
    }

    private void PlacePlayer(PlayerSpawnPoint spawnPoint)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError(
                $"No GameObject tagged Player was found in scene " +
                $"'{SceneManager.GetActiveScene().name}'.",
                this
            );

            return;
        }

        CharacterController characterController = 
            player.GetComponent<CharacterController>();
        
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        player.transform.SetPositionAndRotation(
            spawnPoint.transform.position,
            spawnPoint.transform.rotation
        );

        PlayerInteractionController interactionController =
            player.GetComponent<PlayerInteractionController>();

        interactionController?.ResetInteractionRanges();

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        CameraController.Instance?.SnapToPlayer();
    }

    private bool TryReadSave(out SaveGameData data, out string error)
    {
        data = null;
        error = string.Empty;

        if (!File.Exists(SavePath))
        {
            error = "No save file exists";
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<SaveGameData>(
                File.ReadAllText(SavePath)
            );

            if (data == null)
            {
                error = "Save JSON did not contain valid data";
                return false;
            }

            if (data.saveVersion != CurrentSaveVersion)
            {
                error = $"Unsupported save version '{data.saveVersion}'";
                return false;
            }

            if (!CanLoadScene(data.checkpointSceneName))
            {
                error = 
                    $"Saved scene '{data.checkpointSceneName}' " +
                    "is unavailable";

                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }
    }

    private static bool CanLoadScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) &&
                Application.CanStreamedLevelBeLoaded(sceneName);
    }
}



