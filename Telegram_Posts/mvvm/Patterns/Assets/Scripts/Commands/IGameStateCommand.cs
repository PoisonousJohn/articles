public interface IGameStateCommand : ICommand
{
	void Execute(GameState gameState);
}