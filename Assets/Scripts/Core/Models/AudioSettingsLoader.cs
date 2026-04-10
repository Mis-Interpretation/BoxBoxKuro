using System.IO;
using UnityEngine;

/// <summary>
/// 读取并缓存全局音频配置。
/// </summary>
public static class AudioSettingsLoader
{
    private static AudioSettingsData _cached;
    private static string SettingsPath => Path.Combine(Application.streamingAssetsPath, "audio_settings.json");

    public static AudioSettingsData Load()
    {
        if (_cached != null) return _cached;

        if (!File.Exists(SettingsPath))
        {
            _cached = new AudioSettingsData();
            _cached.EnsureValid();
            return _cached;
        }

        string json = File.ReadAllText(SettingsPath);
        var data = JsonUtility.FromJson<AudioSettingsData>(json) ?? new AudioSettingsData();
        data.EnsureValid();
        _cached = data;
        return _cached;
    }

    public static void Save(AudioSettingsData data)
    {
        if (data == null) data = new AudioSettingsData();
        data.EnsureValid();
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SettingsPath, json);
        _cached = data;
    }
}
