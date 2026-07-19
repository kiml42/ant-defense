using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelRegistry", menuName = "Ant Defense/Level Registry")]
public class LevelRegistry : ScriptableObject
{
    public List<LevelDefinition> Levels;
}
