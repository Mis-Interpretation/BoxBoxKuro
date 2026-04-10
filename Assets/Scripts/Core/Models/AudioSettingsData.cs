/// <summary>
/// 全局音频配置（来自 StreamingAssets/audio_settings.json）。
/// </summary>
[System.Serializable]
public class AudioSettingsData
{
    public const string DefaultLevelCompleteSfxPath = "Sound/SFX/soft-success";

    public string DefaultBgmPath = "";
    public string LevelCompleteSfxPath = DefaultLevelCompleteSfxPath;

    public void EnsureValid()
    {
        if (DefaultBgmPath == null) DefaultBgmPath = "";
        if (string.IsNullOrWhiteSpace(LevelCompleteSfxPath))
            LevelCompleteSfxPath = DefaultLevelCompleteSfxPath;
    }
}
