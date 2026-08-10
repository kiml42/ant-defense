using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Add to every level scene. Loads the base scene additively if it is not
/// already present, so any level can be the entry point regardless of how
/// it was launched (editor play mode, level select, or direct load).
/// </summary>
public class BaseSceneLoader : MonoBehaviour
{
    [Tooltip("Name of the base scene that must be loaded alongside every level.")]
    [SceneName]
    public string BaseSceneName = "BaseScene";

    void Awake()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == BaseSceneName)
                return;
        }
        SceneManager.LoadScene(BaseSceneName, LoadSceneMode.Additive);
    }
}
