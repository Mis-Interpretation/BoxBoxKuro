#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

/// <summary>
/// 从其他场景（如关卡编排）跳转到关卡编辑器时，传递待加载的关卡名。
/// Editor 下用 EditorPrefs（OpenScene 结束 Play 后仍保留）；构建版用 PlayerPrefs。
/// </summary>
public static class LevelEditorPendingLoad
{
    private const string Key = "BoxBoxBox_LevelEditor_PendingLevel";

    public static void SetPendingLevel(string levelName)
    {
        if (string.IsNullOrWhiteSpace(levelName))
            return;

#if UNITY_EDITOR
        EditorPrefs.SetString(Key, levelName);
#else
        PlayerPrefs.SetString(Key, levelName);
        PlayerPrefs.Save();
#endif
    }

    /// <summary>
    /// 读取并清除待加载关卡名；若无有效值则返回 false。
    /// </summary>
    public static bool TryConsumePendingLevel(out string levelName)
    {
        levelName = null;

#if UNITY_EDITOR
        if (!EditorPrefs.HasKey(Key))
            return false;
        levelName = EditorPrefs.GetString(Key);
        EditorPrefs.DeleteKey(Key);
#else
        if (!PlayerPrefs.HasKey(Key))
            return false;
        levelName = PlayerPrefs.GetString(Key);
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
#endif

        return !string.IsNullOrWhiteSpace(levelName);
    }
}
