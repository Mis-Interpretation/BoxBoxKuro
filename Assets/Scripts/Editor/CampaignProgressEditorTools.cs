using UnityEditor;
using UnityEngine;

public static class CampaignProgressEditorTools
{
    [MenuItem("Tools/BoxBoxBox/Clear Campaign Completion")]
    public static void ClearCampaignCompletion()
    {
        CampaignProgressController.ResetProgressAndCompletion();
        Debug.Log("已清空通关记录并重置进度到 1-1。");
    }
}
