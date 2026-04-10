using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 关卡内顶部标题：显示战役编号与关卡显示名（仅文字，无背景）。
/// </summary>
public class LevelTitleHudView : MonoBehaviour
{
    private Label _label;

    private void Awake()
    {
        var doc = GetComponent<UIDocument>();
        _label = doc.rootVisualElement.Q<Label>("level-title-label");
    }

    public void SetTitleText(string text)
    {
        if (_label != null)
            _label.text = text ?? "";
    }
}
