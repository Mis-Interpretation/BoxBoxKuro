#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-shot migration helpers for Unity 2022.3 + URP 14. Invoke via batchmode:
/// -executeMethod MigrationUrpBootstrap.CreateAndWireUrp
/// </summary>
public static class MigrationUrpBootstrap
{
    public static void CreateAndWireUrp()
    {
        const string root = "Assets/Settings";
        const string dir = root + "/2022";

        if (!AssetDatabase.IsValidFolder(root))
            AssetDatabase.CreateFolder("Assets", "Settings");
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder(root, "2022");

        var renderer = ScriptableObject.CreateInstance<Renderer2DData>();
        var rendererPath = $"{dir}/Renderer2D_2022.asset";
        AssetDatabase.CreateAsset(renderer, rendererPath);

        var pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        var so = new SerializedObject(pipeline);
        var list = so.FindProperty("m_RendererDataList");
        list.ClearArray();
        list.InsertArrayElementAtIndex(0);
        list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        so.ApplyModifiedPropertiesWithoutUndo();

        var pipelinePath = $"{dir}/UniversalRP_2022.asset";
        AssetDatabase.CreateAsset(pipeline, pipelinePath);
        AssetDatabase.SaveAssets();

        GraphicsSettings.defaultRenderPipeline = pipeline;

        var saved = QualitySettings.GetQualityLevel();
        for (var i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipeline;
        }

        QualitySettings.SetQualityLevel(saved, false);
        AssetDatabase.SaveAssets();

        Debug.Log($"MigrationUrpBootstrap: wired URP to {pipelinePath} and {rendererPath}.");

        if (Application.isBatchMode)
            EditorApplication.Exit(0);
    }
}
#endif
