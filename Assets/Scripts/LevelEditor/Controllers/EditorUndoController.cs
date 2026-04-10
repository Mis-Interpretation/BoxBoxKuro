using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理编辑器撤销栈，监听 Ctrl+Z 快捷键。
/// </summary>
public class EditorUndoController : MonoBehaviour
{
    private readonly Stack<IEditorCommand> _undoStack = new Stack<IEditorCommand>();

    public void Record(IEditorCommand command)
    {
        _undoStack.Push(command);
    }

    public void Clear()
    {
        _undoStack.Clear();
    }

    private void Update()
    {
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            && Input.GetKeyDown(KeyCode.Z))
        {
            if (_undoStack.Count > 0)
                _undoStack.Pop().Undo();
        }
    }
}
