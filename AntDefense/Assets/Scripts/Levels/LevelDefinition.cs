using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Ant Defense/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    public string LevelName = "New Level";

    [Tooltip("Name of the additive scene to load for this level (must be in Build Settings).")]
    public string SceneName;

    [Header("Player Resources")]
    public float StartingMoney = 100f;
    public float IncomePerSecond = 100f;
}
