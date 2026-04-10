#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// CONSOLA SDF shipped with empty glyph/character tables (broken asset). Noto SDF is fixed via YAML for Unity 2022;
/// CONSOLA needs a one-time repopulation from the TTF.
/// </summary>
public static class TmpConsolaFontRepairMenu
{
    private const string ConsolaSdfPath = "Assets/Art/Font/CONSOLA SDF.asset";
    private const string ConsolaTtfPath = "Assets/Art/Font/CONSOLA.TTF";

    private const string AsciiPrintable =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

    private static void ApplyFontAssetSerializedFields(TMP_FontAsset fa, Font font)
    {
        var so = new SerializedObject(fa);
        var editorRef = so.FindProperty("m_SourceFontFile_EditorRef");
        if (editorRef != null)
            editorRef.objectReferenceValue = font;
        var src = so.FindProperty("m_SourceFontFile");
        if (src != null)
            src.objectReferenceValue = font;
        var guid = so.FindProperty("m_SourceFontFileGUID");
        if (guid != null)
            guid.stringValue = AssetDatabase.AssetPathToGUID(ConsolaTtfPath);

        var w = so.FindProperty("m_AtlasWidth");
        if (w != null) w.intValue = 1024;
        var h = so.FindProperty("m_AtlasHeight");
        if (h != null) h.intValue = 1024;
        var pad = so.FindProperty("m_AtlasPadding");
        if (pad != null) pad.intValue = 9;
        var mode = so.FindProperty("m_AtlasRenderMode");
        if (mode != null)
            mode.intValue = (int)GlyphRenderMode.SDFAA;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/TMP/Repair CONSOLA SDF (repopulate ASCII)")]
    public static void RepairConsola()
    {
        var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ConsolaSdfPath);
        var font = AssetDatabase.LoadAssetAtPath<Font>(ConsolaTtfPath);
        if (fa == null || font == null)
        {
            Debug.LogError("TmpConsolaFontRepairMenu: missing font asset or CONSOLA.TTF.");
            return;
        }

        if (!Application.isBatchMode)
            Undo.RecordObject(fa, "Repair CONSOLA TMP");

        fa.ClearFontAssetData(false);
        ApplyFontAssetSerializedFields(fa, font);

        var prevMode = fa.atlasPopulationMode;
        fa.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        if (!fa.TryAddCharacters(AsciiPrintable, out var missing) || !string.IsNullOrEmpty(missing))
            Debug.LogWarning($"TmpConsolaFontRepairMenu: some glyphs missing: \"{missing}\"");

        fa.atlasPopulationMode = prevMode;
        fa.ReadFontAssetDefinition();
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        Debug.Log("TmpConsolaFontRepairMenu: CONSOLA SDF repair finished.");
    }

    public static void RepairConsolaBatch()
    {
        RepairConsola();
        EditorApplication.Exit(0);
    }
}
#endif
