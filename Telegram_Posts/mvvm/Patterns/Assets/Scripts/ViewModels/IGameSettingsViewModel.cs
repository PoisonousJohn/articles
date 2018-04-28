using MVVM;
using UniRx;
public interface IGameSettingsViewModel : IViewModel
{
    IReactiveProperty<bool> soundEnabled { get; }
    IReactiveProperty<bool> musicEnabled { get; }
    IReactiveProperty<float> musicLevel { get; }
    IReactiveProperty<float> soundLevel { get; }
}