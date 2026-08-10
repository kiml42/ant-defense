using UnityEngine;

/// <summary>
/// Sits in the base scene. Reads the current level from GameState and applies
/// per-level resource settings (money, income). Scene loading is handled by
/// BaseSceneLoader in each level scene instead.
/// </summary>
public class LevelLoader : MonoBehaviour
{
    public GameState GameState;

    void Awake()
    {
        if (GameState == null || GameState.CurrentLevel == null) return;
        ApplyPlayerResources(GameState.CurrentLevel);
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
