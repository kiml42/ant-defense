using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Shows/hides a pause panel when the game is paused. Assign the panel,
/// level name label, and GameState in the Inspector.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public GameObject Panel;
    public TMP_Text LevelNameLabel;
    public GameState GameState;

    [SceneName]
    public string LevelSelectSceneName;

    void OnEnable()
    {
        GlobalKeyHandler.OnModeChanged += OnModeChanged;
    }

    void OnDisable()
    {
        GlobalKeyHandler.OnModeChanged -= OnModeChanged;
    }

    void Start()
    {
        if (LevelNameLabel != null && GameState?.CurrentLevel != null)
            LevelNameLabel.text = GameState.CurrentLevel.LevelName;

        Panel.SetActive(false);
    }

    private void OnModeChanged(GlobalKeyHandler.TimeScaleMode mode)
    {
        Panel.SetActive(mode == GlobalKeyHandler.TimeScaleMode.Paused);
    }

    public void Resume()
    {
        FindFirstObjectByType<GlobalKeyHandler>()?.SetMode(GlobalKeyHandler.TimeScaleMode.Normal);
    }

    public void ExitToMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (GameState != null)
            GameState.CurrentLevel = null;
        Debug.Log($"PauseMenu: Loading scene '{LevelSelectSceneName}'");
        SceneManager.LoadScene(LevelSelectSceneName);
    }
}
