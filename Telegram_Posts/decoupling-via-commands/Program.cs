using System;
using System.Collections.Generic;

namespace decoupling_via_messages
{

    public interface ICommand
    {
        void Execute();
    }

    public interface IGameSystem
    {
        void Enqueue(ICommand cmd);
    }

    public class GameSystem : IGameSystem
    {
        public void Enqueue(ICommand cmd)
        {
            _cmds.Add(cmd);
        }

        public void ProcessCommands()
        {
            foreach (var cmd in _cmds)
            {
                cmd.Execute();
            }
        }

        private List<ICommand> _cmds = new List<ICommand>();
    }

    public class DoubleBufferGameSystem : IGameSystem
    {
        public void Enqueue(ICommand cmd)
        {
            _cmds.Add(cmd);
        }

        public void ProcessCommands()
        {
            if (_cmds.Count == 0)
            {
                return;
            }

            var oldCmds = _runningCmds;
            _runningCmds = _cmds;

            foreach (var cmd in _runningCmds)
            {
                cmd.Execute();
            }

            _cmds = oldCmds;
            _cmds.Clear();
        }

        private List<ICommand> _cmds = new List<ICommand>();
        private List<ICommand> _runningCmds = new List<ICommand>();
    }

    public class WriteToConsoleCommand : ICommand
    {
        public string Message { get; private set; }
        public WriteToConsoleCommand(string msg)
        {
            Message = msg;
        }
        public void Execute()
        {
            Console.WriteLine(Message);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var gameSystem = new GameSystem();
            var cmd = new WriteToConsoleCommand("Hello world");
            var cmd2 = new WriteToConsoleCommand("Hello world2");
            gameSystem.Enqueue(cmd);
            gameSystem.ProcessCommands();
            gameSystem.Enqueue(cmd2);
            gameSystem.ProcessCommands();
        }
    }
}
