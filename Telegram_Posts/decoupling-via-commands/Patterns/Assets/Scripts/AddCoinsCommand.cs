using Newtonsoft.Json;

public class AddCoinsCommand : IGameStateCommand
{

    public AddCoinsCommand(int amount)
    {
        _amount = amount;
    }

    public void Execute(GameState gameState)
    {
        gameState.coins += _amount;
        // this is the fix
        if (gameState.coins < 0)
        {
            gameState.coins = 0;
        }
    }

    public override string ToString() {
        return GetType().ToString() + " " + _amount;
    }

    [JsonProperty("amount")]
    private int _amount;
}