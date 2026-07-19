using UnityEngine;

[CreateAssetMenu(fileName = "NewLevel", menuName = "Ant Defense/Level Definition")]
public class LevelDefinition : ScriptableObject
{
    public string LevelName = "New Level";

    [SceneName]
    public string SceneName;

    [Header("Player Resources")]
    public float StartingMoney = 100f;
    public float IncomePerSecond = 100f;
}
