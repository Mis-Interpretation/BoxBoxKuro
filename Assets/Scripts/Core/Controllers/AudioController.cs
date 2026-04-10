using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局音频控制器：提供 BGM/SFX 播放能力。
/// </summary>
public class AudioController : MonoBehaviour
{
    private static AudioController _instance;

    private AudioSource _bgmSource;
    private AudioSource _sfxSource;
    private string _currentBgmPath = "";
    private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();

    public static AudioController Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindAnyObjectByType<AudioController>();
            if (_instance != null) return _instance;

            var go = new GameObject("AudioController");
            _instance = go.AddComponent<AudioController>();
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.playOnAwake = false;
        _bgmSource.loop = true;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.loop = false;
    }

    public void PlayBgm(string bgmPath, bool loop = true)
    {
        string normalized = NormalizePath(bgmPath);
        if (IsNone(normalized))
        {
            StopBgm();
            return;
        }

        if (_bgmSource.isPlaying && _currentBgmPath == normalized) return;

        var clip = LoadClip(normalized);
        if (clip == null) return;

        _bgmSource.loop = loop;
        _bgmSource.clip = clip;
        _bgmSource.Play();
        _currentBgmPath = normalized;
    }

    public void PlayDefaultBgm()
    {
        PlayBgm(AudioSettingsLoader.Load().DefaultBgmPath, true);
    }

    public void PlaySfx(string sfxPath)
    {
        string normalized = NormalizePath(sfxPath);
        if (IsNone(normalized)) return;

        var clip = LoadClip(normalized);
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip);
    }

    public void PlayLevelCompleteSfx()
    {
        PlaySfx(AudioSettingsLoader.Load().LevelCompleteSfxPath);
    }

    public void StopBgm()
    {
        _bgmSource.Stop();
        _bgmSource.clip = null;
        _currentBgmPath = "";
    }

    private AudioClip LoadClip(string resourcePath)
    {
        if (_clipCache.TryGetValue(resourcePath, out var cached))
            return cached;

        var clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null)
        {
            Debug.LogWarning($"[Audio] 找不到音频资源: {resourcePath}");
            return null;
        }

        _clipCache[resourcePath] = clip;
        return clip;
    }

    private static bool IsNone(string path)
    {
        return string.IsNullOrWhiteSpace(path) || path == "none";
    }

    private static string NormalizePath(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        string path = raw.Trim();
        if (path.ToLowerInvariant() == "none") return "none";

        // 容错：允许配置里误写文件扩展名（Resources.Load 需要无扩展名路径）
        string lower = path.ToLowerInvariant();
        if (lower.EndsWith(".ogg") || lower.EndsWith(".mp3") || lower.EndsWith(".wav"))
            path = path.Substring(0, path.LastIndexOf('.'));

        return path;
    }
}
