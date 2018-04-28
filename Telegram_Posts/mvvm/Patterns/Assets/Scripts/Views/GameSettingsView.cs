using UnityEngine;
using UnityEngine.UI;
using MVVM;
using BindingsRx.Bindings;
using Zenject;
using UniRx;
using System;

public class GameSettingsView : BaseMvvmView<IGameSettingsViewModel>
{
	public Toggle soundEnabled;
	public Toggle musicEnabled;
	public Slider musicLevel;
	public Slider soundLevel;

    [Inject]
    public override void Init(IGameSettingsViewModel viewModel)
    {
        soundLevel
            .OnValueChangedAsObservable()
            .Throttle(TimeSpan.FromSeconds(1))
            .Subscribe(i => new object())
            .AddTo(gameObject);
        musicEnabled.BindToggleTo(viewModel.musicEnabled);
        soundEnabled.BindToggleTo(viewModel.soundEnabled);
        soundLevel.BindValueTo(viewModel.soundLevel);
        musicLevel.BindValueTo(viewModel.musicLevel);
    }
}