using UniRx;

public class GameSettingsViewModelStub : IGameSettingsViewModel
{
    public IReactiveProperty<bool> soundEnabled { get; private set; }
    public IReactiveProperty<bool> musicEnabled { get; private set; }
    public IReactiveProperty<float> musicLevel { get; private set; }
    public IReactiveProperty<float> soundLevel { get; private set; }

    public GameSettingsViewModelStub()
    {
        soundEnabled = new ReactiveProperty<bool>(true);
        musicEnabled = new ReactiveProperty<bool>(true);
        musicLevel = new ReactiveProperty<float>(0.5f);
        soundLevel = new ReactiveProperty<float>(0.5f);
    }

}