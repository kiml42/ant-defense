using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SceneNameAttribute))]
public class SceneNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "SceneName requires a string field.");
            return;
        }

        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        var sceneNames = new string[scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            sceneNames[i] = System.IO.Path.GetFileNameWithoutExtension(scenes[i].path);
        }

        var currentIndex = System.Array.IndexOf(sceneNames, property.stringValue);
        if (currentIndex < 0) currentIndex = 0;

        EditorGUI.BeginProperty(position, label, property);
        var selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, sceneNames);
        property.stringValue = sceneNames[selectedIndex];
        EditorGUI.EndProperty();
    }
}
