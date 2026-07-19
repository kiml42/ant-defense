using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Sits in the base scene. Applies per-level resource settings then loads the
/// level's own scene additively so gameplay objects (nests, food, etc.) are
/// kept separate from shared infrastructure.
/// </summary>
public class LevelLoader : MonoBehaviour
{
    public LevelDefinition Level;

    void Awake()
    {
        if (Level == null)
        {
            Debug.LogWarning("LevelLoader: No LevelDefinition assigned.");
            return;
        }

        ApplyPlayerResources();
    }

    IEnumerator Start()
    {
        if (Level == null) yield break;

        if (string.IsNullOrEmpty(Level.SceneName))
        {
            Debug.LogWarning("LevelLoader: LevelDefinition has no SceneName set.");
            yield break;
        }

        yield return SceneManager.LoadSceneAsync(Level.SceneName, LoadSceneMode.Additive);
    }

    private void ApplyPlayerResources()
    {
        if (MoneyTracker.Instance != null)
        {
            MoneyTracker.Instance.InitialMoney = Level.StartingMoney;
            MoneyTracker.Instance.IncomePerSecond = Level.IncomePerSecond;
        }
    }
}
