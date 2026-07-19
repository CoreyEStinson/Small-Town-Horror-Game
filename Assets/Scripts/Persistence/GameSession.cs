using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public GameState GameState { get; private set; }
    public SaveManager SaveManager { get; private set; }
    public FadeTransition FadeTransition { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        GameState = EnsureComponent<GameState>();
        FadeTransition = EnsureComponent<FadeTransition>();
        SaveManager = EnsureComponent<SaveManager>();
        EnsureComponent<SaveDebugInput>();
    }

    private T EnsureComponent<T>() where T : Component
    {
        T component = GetComponent<T>();

        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }
}
