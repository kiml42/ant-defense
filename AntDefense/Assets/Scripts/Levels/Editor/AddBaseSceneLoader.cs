#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AddBaseSceneLoader
{
    [MenuItem("AntDefense/Add BaseSceneLoader to Current Scene")]
    static void Run()
    {
        var scene = EditorSceneManager.GetActiveScene();

        foreach (var go in scene.GetRootGameObjects())
        {
            if (go.GetComponent<BaseSceneLoader>() != null)
            {
                Debug.Log($"BaseSceneLoader already present in {scene.name}.");
                return;
            }
        }

        var newGO = new GameObject("BaseSceneLoader");
        newGO.AddComponent<BaseSceneLoader>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Added BaseSceneLoader to {scene.name}.");
    }
}
#endif
