using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class CoinsView : MonoBehaviour
{
    public Text currencyText;

    [Inject]
    public void Init(IGameStateCommandsExecutor commandsExecutor)
    {
        _commandsExecutor = commandsExecutor;
        _commandsExecutor.stateUpdated += UpdateView;
    }

    public void AddCoins()
    {
        var cmd = new AddCoinsCommand(Random.Range(1, 100));
        _commandsExecutor.Execute(cmd);
    }

    public void RemoveCoins()
    {
        var cmd = new AddCoinsCommand(-Random.Range(1, 100));
        _commandsExecutor.Execute(cmd);
    }

    public void UpdateView(GameState gameState)
    {
        currencyText.text = "Coins: " + gameState.coins;
    }

    private void OnDestroy()
    {
        _commandsExecutor.stateUpdated -= UpdateView;
    }

    private IGameStateCommandsExecutor _commandsExecutor;
}