using UnityEngine;

[CreateAssetMenu(fileName = "GameState", menuName = "Ant Defense/Game State")]
public class GameState : ScriptableObject
{
    public LevelDefinition CurrentLevel;
}
