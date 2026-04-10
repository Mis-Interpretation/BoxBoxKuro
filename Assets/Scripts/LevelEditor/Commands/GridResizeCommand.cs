using UnityEngine;

/// <summary>
/// 记录网格尺寸变更，可撤销。
/// </summary>
public class GridResizeCommand : IEditorCommand
{
    private readonly EditorStateModel _state;
    private readonly EditorHUDView _hud;
    private readonly int _oldWidth;
    private readonly int _oldHeight;

    public GridResizeCommand(EditorStateModel state, EditorHUDView hud, int oldWidth, int oldHeight)
    {
        _state = state;
        _hud = hud;
        _oldWidth = oldWidth;
        _oldHeight = oldHeight;
    }

    public void Undo()
    {
        _state.CurrentLevel.Width = _oldWidth;
        _state.CurrentLevel.Height = _oldHeight;
        if (_hud != null) _hud.RefreshGridSizeUI();
    }
}
