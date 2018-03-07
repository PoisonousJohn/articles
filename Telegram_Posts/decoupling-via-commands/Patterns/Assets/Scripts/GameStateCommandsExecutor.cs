using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public interface ICommandsExecutor<TState, TCommand>
    where TCommand : ICommand
{
    event System.Action<TState> stateUpdated;
    void Execute(TCommand command);
}

public interface IGameStateCommandsExecutor : ICommandsExecutor<GameState, IGameStateCommand>
{

}

public class DefaultCommandsExecutor : IGameStateCommandsExecutor
{
    public event Action<GameState> stateUpdated
    {
        add
        {
            _stateUpdated += value;
            if (value != null)
            {
                value(_gameStateManager.GameState);
            }
        }
        remove
        {
            _stateUpdated -= value;
        }
    }

    public DefaultCommandsExecutor(IGameStateManager gameStateManager)
    {
        _gameStateManager = gameStateManager;
    }

    public virtual void Execute(IGameStateCommand command)
    {
        command.Execute(_gameStateManager.GameState);
        if (_stateUpdated != null)
        {
            _stateUpdated(_gameStateManager.GameState);
        }
    }

    protected readonly IGameStateManager _gameStateManager;
    protected Action<GameState> _stateUpdated;

}
public class DebugCommandsExecutor : DefaultCommandsExecutor
{
    public IList<IGameStateCommand> commandsHistory { get { return _commands; } }
    public DebugCommandsExecutor(DebugGameStateManager gameStateManager)
        : base(gameStateManager)
    {
        _debugGameStateManager = gameStateManager;
    }

    public void SaveReplay(string name)
    {
        _debugGameStateManager.SaveBackupAs(name);
        File.WriteAllText(GetReplayFile(name),
                            JsonConvert.SerializeObject(new CommandsHistory { commands = _commands },
                                                        _jsonSettings));
    }

    public void LoadReplay(string name)
    {
        _debugGameStateManager.RestoreBackupState(name);
        _commands = JsonConvert.DeserializeObject<CommandsHistory>(
                        File.ReadAllText(GetReplayFile(name)),
                        _jsonSettings
                    ).commands;
        _stateUpdated(_gameStateManager.GameState);
    }

    public void Replay(string name, int toIndex)
    {
        _debugGameStateManager.RestoreBackupState(name);
        LoadReplay(name);
        var history = _commands;
        _commands = new List<IGameStateCommand>();
        for (int i = 0; i < Math.Min(toIndex, history.Count); ++i)
        {
            Execute(history[i]);
        }
        _commands = history;
    }

    private string GetReplayFile(string name)
    {
        return  Path.Combine(Application.persistentDataPath, name + "_commands.json");
    }

    public override void Execute(IGameStateCommand command)
    {
        _commands.Add(command);
        base.Execute(command);
    }

    private List<IGameStateCommand> _commands = new List<IGameStateCommand>();

    public class CommandsHistory
    {
        public List<IGameStateCommand> commands;
    }

    private readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings() {
        TypeNameHandling = TypeNameHandling.All
    };
    private readonly DebugGameStateManager _debugGameStateManager;
}