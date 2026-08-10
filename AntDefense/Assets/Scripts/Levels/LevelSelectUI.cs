using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Spawns one button per level in the registry. Assign to a GameObject in
/// the LevelSelect scene alongside a LevelRegistry and GameState asset.
/// Each level scene is responsible for loading BaseScene via BaseSceneLoader.
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    public LevelRegistry Registry;
    public GameState GameState;

    [Tooltip("Parent transform to spawn buttons into.")]
    public Transform ButtonContainer;

    [Tooltip("Button prefab. Must have a Text or TMP_Text child for the label.")]
    public Button ButtonPrefab;

    void Start()
    {
        foreach (var level in Registry.Levels)
        {
            var btn = Instantiate(ButtonPrefab, ButtonContainer);

            var label = btn.GetComponentInChildren<TMPro.TMP_Text>();
            if (label != null)
                label.text = level.LevelName;

            var captured = level;
            btn.onClick.AddListener(() => SelectLevel(captured));
        }
    }

    private void SelectLevel(LevelDefinition level)
    {
        GameState.CurrentLevel = level;
        SceneManager.LoadScene(level.SceneName);
    }
}
