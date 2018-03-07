using UnityEngine;
using Zenject;

public class BindingsInstaller : MonoInstaller<BindingsInstaller>
{
    public override void InstallBindings()
    {
        Container.Bind<Loader>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
    #if DEBUG
        Container.Bind<IGameStateManager>().To<DebugGameStateManager>().AsSingle();
        Container.Bind<DebugGameStateManager>().AsSingle();
        Container.Bind<IGameStateCommandsExecutor>().To<DebugCommandsExecutor>().AsSingle();
    #else
        Container.Bind<IGameStateManager>().To<LocalGameStateManager>().AsSingle();
        Container.Bind<IGameStateCommandsExecutor>().To<DefaultCommandsExecutor>().AsSingle();
    #endif
    }
}