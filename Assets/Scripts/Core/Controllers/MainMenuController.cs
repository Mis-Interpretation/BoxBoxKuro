using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 主菜单控制器：绑定 Play 和 Edit Levels 按钮。
/// 挂载在 MainScene 中包含按钮的 Canvas 或父对象上。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _editLevelsButton;
    [SerializeField] private Button _entityConfigButton;

    private void Start()
    {
        // 如果没手动赋值，尝试自动查找
        if (_playButton == null)
        {
            var go = GameObject.Find("Button_Play");
            if (go != null) _playButton = go.GetComponent<Button>();
        }
        if (_editLevelsButton == null)
        {
            var go = GameObject.Find("Button_EditLevel");
            if (go != null) _editLevelsButton = go.GetComponent<Button>();
        }
        if (_entityConfigButton == null)
        {
            var go = GameObject.Find("Button_EntityConfig");
            if (go != null) _entityConfigButton = go.GetComponent<Button>();
        }

        if (_playButton != null)
            _playButton.onClick.AddListener(OnPlay);
        if (_editLevelsButton != null)
            _editLevelsButton.onClick.AddListener(OnEditLevels);
        if (_entityConfigButton != null)
            _entityConfigButton.onClick.AddListener(OnEntityConfig);
    }

    private void OnPlay()
    {
        SceneManager.LoadScene(SceneNameModel.SelectScene);
    }

    private void OnEditLevels()
    {
        SceneManager.LoadScene(SceneNameModel.ArrangeScene);
    }

    private void OnEntityConfig()
    {
        SceneManager.LoadScene(SceneNameModel.EntityConfigScene);
    }
}
