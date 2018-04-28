using UnityEngine;
using Zenject;
using MVVM;

public class MockBindingsInstaller : MonoInstaller<MockBindingsInstaller>
{
    public override void InstallBindings()
    {
        Container.Bind<IGameSettingsViewModel>().To<GameSettingsViewModelStub>().AsSingle();
    }
}