using System.Collections.Generic;

public class CompositeCommand : IEditorCommand
{
    private readonly List<IEditorCommand> _commands;

    public CompositeCommand(List<IEditorCommand> commands)
    {
        _commands = commands;
    }

    public void Undo()
    {
        for (int i = _commands.Count - 1; i >= 0; i--)
            _commands[i].Undo();
    }
}
