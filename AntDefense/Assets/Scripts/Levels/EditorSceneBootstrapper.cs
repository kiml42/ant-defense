using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads BaseScrene additively when a level scene is played directly in the editor.
/// In production this Awake does nothing; BaseScrene is always loaded first via the
/// normal flow (LevelSelectUI → LevelLoader).
/// </summary>
public class EditorSceneBootstrapper : MonoBehaviour
{
    [Tooltip("Name of the base scene that must be loaded alongside every level.")]
    public string BaseSceneName = "BaseScrene";

    void Awake()
    {
#if UNITY_EDITOR
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == BaseSceneName)
                return;
        }
        SceneManager.LoadScene(BaseSceneName, LoadSceneMode.Additive);
#endif
    }
}
