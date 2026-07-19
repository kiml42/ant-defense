using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sits in the base scene. Reads the current level from GameState (set by the
/// level select screen), falls back to FallbackLevel for in-editor testing.
/// Applies per-level resource settings then loads the level scene additively.
/// </summary>
public class LevelLoader : MonoBehaviour
{
    public GameState GameState;

    [Tooltip("Used when GameState has no CurrentLevel set, e.g. when entering play mode directly from the base scene.")]
    public LevelDefinition FallbackLevel;

    void Awake()
    {
        var level = ResolveLevel();
        if (level == null)
        {
            Debug.LogWarning("LevelLoader: No level to load. Assign a FallbackLevel or run from the level select scene.");
            return;
        }

        ApplyPlayerResources(level);
    }

    IEnumerator Start()
    {
        var level = ResolveLevel();
        if (level == null) yield break;

        if (string.IsNullOrEmpty(level.SceneName))
        {
            Debug.LogWarning($"LevelLoader: LevelDefinition '{level.LevelName}' has no SceneName set.");
            yield break;
        }

        yield return SceneManager.LoadSceneAsync(level.SceneName, LoadSceneMode.Additive);
    }

    private LevelDefinition ResolveLevel()
    {
        if (GameState != null && GameState.CurrentLevel != null)
            return GameState.CurrentLevel;
        return FallbackLevel;
    }

    private void ApplyPlayerResources(LevelDefinition level)
    {
        if (MoneyTracker.Instance != null)
        {
            MoneyTracker.Instance.InitialMoney = level.StartingMoney;
            MoneyTracker.Instance.IncomePerSecond = level.IncomePerSecond;
        }
    }
}
