#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Batch smoke: open PlayLevel and count missing scripts (no Play Mode).
/// -executeMethod MigrationPlayLevelVerify.VerifyPlayLevelScene
/// </summary>
public static class MigrationPlayLevelVerify
{
    private const string PlayLevelPath = "Assets/Scenes/PlayLevel.unity";

    public static void VerifyPlayLevelScene()
    {
        var scene = EditorSceneManager.OpenScene(PlayLevelPath, OpenSceneMode.Single);
        var missing = 0;
        foreach (var root in scene.GetRootGameObjects())
            missing += CountMissingScriptsRecursive(root);

        if (missing > 0)
        {
            Debug.LogError($"MigrationPlayLevelVerify: {missing} missing script(s) under root objects in {PlayLevelPath}.");
            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            return;
        }

        Debug.Log("MigrationPlayLevelVerify: PlayLevel opened with no missing scripts (full hierarchy scan).");
        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }

    private static int CountMissingScriptsRecursive(GameObject go)
    {
        var n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        var t = go.transform;
        for (var i = 0; i < t.childCount; i++)
            n += CountMissingScriptsRecursive(t.GetChild(i).gameObject);
        return n;
    }
}
#endif
