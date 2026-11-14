using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class StartupSceneSetup
{
    const string LoadingScenePath = "Assets/Scenes/Loading.unity";

    static StartupSceneSetup()
    {
        EditorApplication.delayCall += Apply;
    }

    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LoadingScenePath);
        if (scene == null || EditorSceneManager.playModeStartScene == scene)
            return;

        EditorSceneManager.playModeStartScene = scene;
    }
}
