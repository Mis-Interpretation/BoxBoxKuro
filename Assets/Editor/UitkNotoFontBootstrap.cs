#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;

/// <summary>
/// Player 中 UI Toolkit 需要 TextCore <see cref="FontAsset"/>；不能把 TMP 的 TMP_FontAsset 赋给 -unity-font-definition，否则文字会全部不显示。
/// 在工程内从 Noto TTF 生成一份 UITK 专用 FontAsset，并由 <c>ProjectUiDefaultFont.uss</c> 在 runtime theme 里全局引用。
/// </summary>
[InitializeOnLoad]
public static class UitkNotoFontBootstrap
{
    const string TtfPath = "Assets/Art/Font/NotoSansSC-Regular.ttf";
    const string FontAssetPath = "Assets/Art/Font/NotoSansSC-Regular_UITK.asset";
    const string ThemeUssPath = "Assets/UI Toolkit/UnityThemes/ProjectUiDefaultFont.uss";
    const string ThemeTssPath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";

    static UitkNotoFontBootstrap()
    {
        EditorApplication.delayCall += TryEnsureFontAsset;
    }

    static void TryEnsureFontAsset()
    {
        if (Application.isBatchMode)
            return;

        if (AssetDatabase.LoadAssetAtPath<FontAsset>(FontAssetPath) != null)
            return;

        var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (font == null)
        {
            Debug.LogError($"[BoxBoxKuro] UI Toolkit: 未找到 Noto 源字体，无法生成 TextCore FontAsset：{TtfPath}");
            return;
        }

        FontAsset fa = FontAsset.CreateFontAsset(
            font,
            samplingPointSize: 48,
            atlasPadding: 5,
            renderMode: GlyphRenderMode.SDFAA,
            atlasWidth: 4096,
            atlasHeight: 4096,
            atlasPopulationMode: AtlasPopulationMode.Dynamic);

        if (fa == null)
        {
            Debug.LogError("[BoxBoxKuro] UI Toolkit: FontAsset.CreateFontAsset 返回 null（请确认 TTF 已勾选 Include Font Data）。");
            return;
        }

        fa.name = "NotoSansSC-Regular_UITK";

        AssetDatabase.CreateAsset(fa, FontAssetPath);
        if (fa.material != null)
            AssetDatabase.AddObjectToAsset(fa.material, fa);
        if (fa.atlasTextures != null)
        {
            foreach (var tex in fa.atlasTextures)
            {
                if (tex != null)
                    AssetDatabase.AddObjectToAsset(tex, fa);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ThemeUssPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset(ThemeTssPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        Debug.Log($"[BoxBoxKuro] 已生成 UI Toolkit 用 TextCore FontAsset（请勿用 TMP 资源替代）：{FontAssetPath}");
    }

    [MenuItem("Tools/UI Toolkit/Regenerate Noto Font Asset (TextCore / UITK)")]
    static void RegenerateMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<FontAsset>(FontAssetPath) != null)
            AssetDatabase.DeleteAsset(FontAssetPath);
        AssetDatabase.Refresh();
        TryEnsureFontAsset();
    }
}
#endif
