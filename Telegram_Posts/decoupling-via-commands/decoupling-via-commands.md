# Развязываем игровой код с помощью паттерна Command, и дебажим, летая на машине времени

[![Картинка для привлечения внимания: > Replay bug-10492; going back in time](https://github.com/PoisonousJohn/articles/raw/master/Telegram_Posts/decoupling-via-commands/images/time-machine.png)](https://habrahabr.ru/post/350630/)

Привет! Я пишу статьи, посвященые архитектуре в игровой разработке. В этой статье я хочу разобрать паттерн [Команда (Command)](https://ru.wikipedia.org/wiki/%D0%9A%D0%BE%D0%BC%D0%B0%D0%BD%D0%B4%D0%B0_(%D1%88%D0%B0%D0%B1%D0%BB%D0%BE%D0%BD_%D0%BF%D1%80%D0%BE%D0%B5%D0%BA%D1%82%D0%B8%D1%80%D0%BE%D0%B2%D0%B0%D0%BD%D0%B8%D1%8F)). Он многогранен, и может быть применен по-разному. Но я покажу, как сделать мой любимый трюк -- машина времени для отладки изменений гейм стейта.

Эта штука сэкономила мне кучу времени в поиске и воспроизведении сложных багов. Она позволяет делать "снапшоты" игрового состояния, историю его изменения, и пошагово их применять.

Начинающие разработчики познакомятся с паттерном, а продвинутые, возможно, найдут трюк полезным.

Хотите узнать как это сделать? Прошу под кат.
<cut />
Если вы уже знакомы с паттерном Command, то сразу переходите к секции "Делаем модификацию стейта однонаправленной".

## Паттерн Command

Что мы понимаем под словом "Команда"? Это что-то вроде приказа. С помощью команды человек выражает необходимость в совершении какого-либо действия. Действие -- неотделимо от команды.

Паттерн Command -- это способ представить действие в мире объектно-ориентированного программирования. И именно благодаря полиморфизму это становится возможным.

Идея паттерна в том, что все команды для системы одинаковы. В понятиях ООП все команды имеют общий интерфейс. Система может прозрачно исполнять любую из них. А это значит, что команда должна быть абсолютно самостоятельной, и инкапсулировать в себе все необходимые для ее исполнения данные.

Пока описание довольно абстрактно. Давайте перейдем к конкретике. Базовый интерфейс для всех команд:

```csharp
public interface ICommand
{
    void Execute();
}
```

Теперь пример конкретной реализации команды:

```csharp
public class WriteToConsoleCommand : ICommand
{
    public string Message { get; private set; }
    public void Execute() {
        Console.WriteLine(Message);
    }
}
```

Это эдакий "Hello world" на командах. А как же их исполнять? Напишем простую систему обработки команд.

```csharp
public interface IGameSystem
{
    void Execute(ICommand cmd);
}

public class LoggableGameSystem : IGameSystem
{
    public LoggableGameSystem(ILogger log)
    {
        _log = log;
    }

    public void Execute(ICommand cmd) {
        _log.Debug(string.Format("Executing command <{0}>: {1}", cmd.GetType(), cmd);
        cmd.Execute();
    }

    private ILogger _log;
}
```

Теперь мы можем логгировать каждую исполняемую команду для отладки. Удобно же? Но команду нужно подготовить к дебажному выводу, добавим метод ToString().

```csharp
public class WriteToConsoleCommand : ICommand
{
    public string Message { get; private set; }
    public void Execute() {
        Console.WriteLine(Message);
    }

    public override string ToString()
    {
        return Message;
    }
}
```

Проверим как оно работает.

```csharp
    class Program
    {
        static void Main(string[] args)
        {
            var gameSystem = new LoggableGameSystem();
            var cmd = new WriteToConsoleCommand("Hello world");
            var cmd2 = new WriteToConsoleCommand("Hello world2");
            gameSystem.Execute(cmd);
            gameSystem.Execute(cmd2);
        }
    }
```

Это довольно простой пример. Конечно, дебажный вывод полезен, но не понятно что еще полезного можно извлечь из этого паттерна.

В своих проектах я постоянно использую этот паттерн по нескольким причинам:

* В команде сохраняется все, что необходимо до ее исполнения. Она, по сути, иммутабельный объект. Поэтому ее легко передавать по сети, и одинаково исполнять как на клиенте, так и на сервере. Конечно, это при условии, что при одинаковых входных параметрах и клиент, и сервер, дают одни и те же результаты.
* Команда представляет собой очень маленький кусочек логики. Ее легко писать, легко понимать, и легко отлаживать. Так как команда иммутабельна, и не содержит никаких дополнительных зависимостей, для нее легко писать unit-тесты.
* Сложную бизнес логику легко выражать посредством набора простейших команд. Команды легко переиспользовать и компоновать в последовательности.
* Команда может выступать чекпойнтом, ну или транзакцией, как вам больше нравится. Если изменение состояния данных происходит только посредством команд, это упрощает отладку, да и понимание программы. Если что-то сломалось, вы всегда можете проследить какая команда привела к ошибке. Что удобно -- можно видеть и параметры, с которыми была выполнена команда.
* Выполнение команд может быть отложенным. Типичный пример -- отправка команды на сервер. Когда пользователь инициировал какое-либо действие в игре, создается команда, и добавляется в очередь на исполнение. Фактическое же исполнение команды происходит только после подтверждения от сервера.
* Так как команды достаточно абстрагированы от всех зависимостей, легко менять архитектуру. Например, если раньше код был только оффлайнов и управление AI происходило только локально, то его легко поменять на управление AI с помощью сервера. Ведь коду без разницы кто отправляет команды, локальный код, или сервер.
* Известная фишка команд -- можно не только применять изменения, но и сделать поддержку "отмены" действия
* Код, написанный с идеологией команд, немного отличается от традиционного подхода с вызовом функций. Когда программист создает команду, он сообщает о необходимости изменить состояние. Как и когда это будет сделано -- его не интересует. Это позволяет творить интересные вещи.

Немного подробнее про последний пункт. Например, у вас была синхронная функция, которая должна стать асинхронной. Чтобы ее сделать это, вам необходимо изменить ее сигнатуру, и написать механизм обработки асинхронного результата в виде коллбека, или корутины, или async/await (если вы переползли на .net 4.6). И так каждый раз, для каждой отдельно взятой функции.

Механизм команд позволяет абстрагироваться от механизма исполнения. Поэтому если команда раньше исполнялась моментально, ее легко можно сделать асинхронной. Это даже можно менять динамичеки, в рантайме.

Конкретный пример. Игра поддерживает частичный оффлайн. Если сейчас сетевое соединение недоступно, то команды попадают в очередь, и исполняются в момент восстановления соединения. Если соединение есть, то команды исполняются моментально.

## Делаем модификацию стейта однонаправленной

### Теория

Что за однонаправленная модификация стейта? Идея позаимствованна из подхода [Flux](https://github.com/facebook/flux/tree/master/), описанного ребятами из Facebook. На этом же подходе строятся всякие новомодные библиотеки типа [Redux](https://redux.js.org).

В традиционных MV* подходах, View взаимодействуют с моделью в двустороннем порядке.

В Unity ситуация зачастую еще хуже. Традиционный MVC тут не подходит, и данные часто модифицируют прямо из View, как я это покажу ниже. В сложных приложениях количество связей зашкаливает, апдейт теряется в апдейте, все запутывается, и получается спагетти.

![Взаимодействие с представления с моделями в MV* архитектурах](https://cdn-images-1.medium.com/max/800/1*6wApxobF9eKUe3l1jP8j4A.png)

(Источник: [medium.com](https://medium.com/e-fever/revised-qml-application-architecture-guide-with-flux-a1de143fe13e))

Я предлагаю систему, похожую на Redux. Основная идея, что Redux предлагает хранить все состояние приложения в одном объекте. То есть одной модели.

Некоторые тут ужаснутся. Но ведь сериализация игрового состояния, чаще всего, и сводится к сериализации одного объекта. Это довольно естественный подход для игр.

Вторая идея в том, что состояние модифицируется с помощью Action'ов. По сути -- это ровно то же, что и Command, описанный ранее. View не может модифцировать состояние напрямую, а только посредством команды.

Третья идея -- естественное продолжение, View может только читать состояние и подписываться на его обновления.

Вот как это выглядит в идеологии Flux:

![Поток данных в идеологии Flux](https://cdn-images-1.medium.com/max/800/1*g2DHWCJi7jaEY_VN-mWMbQ.png)

(Источник: [medium.com](https://medium.com/e-fever/revised-qml-application-architecture-guide-with-flux-a1de143fe13e))

В нашем случае Store -- это игровое состояние. А Action -- команда. Dispatcher, соответственно, то, что исполняет команды.

Такой подход даст много плюшек. Так как объект состояния всего один, а его модификация производится только через команды, то легко сделать единственное событие об обновлении состояния.

Тогда UI лего сделать реактивным. То есть автоматически обновлять данные при обновлении стейта (привет [UniRx](https://github.com/neuecc/UniRx), его применение рассмотрим в другой статье).

С таким подходом изменение состояния игры может быть инициировано и с серверной стороны. Так же, через команды. Так как событие об обновлении состояние ровно то же, то UI абсолютно фиолетово, откуда пришел апдейт.

Еще одна плюшка -- крутые возможности по отладке. Так как View может только рожать команды, то трекать изменения стейта становится проще паренной репы.

Детальное логирование, история команд, воспроизведение багов, и т.д., все это становится возможным благодаря такому паттерну.

### Реализация

Для начала опредилимся с состоянием игры. Пусть это будет следующий класс:

```csharp
    [System.Serializable]
    public class GameState
    {
        public int coins;
    }
```

Добавим сохранение состояния игры в файл формата JSON. Для этого сделаем отдельный менеджер.

```csharp
public interface IGameStateManager
{
    GameState GameState { get; set; }
    void Load();
    void Save();
}

public class LocalGameStateManager : IGameStateManager
{
    public GameState GameState { get; set; }

    public void Load()
    {
        if (!File.Exists(GAME_STATE_PATH))
        {
            return;
        }
        GameState = JsonUtility.FromJson<GameState>(File.ReadAllText(GAME_STATE_PATH));
    }

    public void Save()
    {
        File.WriteAllText(GAME_STATE_PATH, JsonUtility.ToJson(GameState));
    }

    private static readonly string GAME_STATE_PATH = Path.Combine(Application.persistentDataPath, "gameState.json"); }
```

В [предыдущей статье](http://goo.gl/NP9Yfy) я рассматривал проблему зависимостей, и говорил о паттерне Dependency Injection (DI). Настало время его использовать.

Для Unity3d есть простой и удобный DI фреймворк [Zenject](https://github.com/modesttree/Zenject). Его и буду использовать. Установка и настройка довольно трививальны, и описаны подробно в документации. Поэтому сразу к делу. Объявим байндинг для IGameStateManager.

Я создал свой экземпляр `MonoInstaller` под названием `BindingsInstaller`, согласно документации, и добавил его на сцену.

```csharp
public class BindingsInstaller : MonoInstaller<BindingsInstaller>
{
    public override void InstallBindings()
    {
        Container.Bind<IGameStateManager>().To<LocalGameStateManager>().AsSingle();
        Container.Bind<Loader>().FromNewComponentOnNewGameObject().NonLazy();
    }
```

Так же я добавил байндинг для компонента Loader, который будет следить за загрузкой и выходом из игры.

```csharp
public class Loader : MonoBehaviour {

    [Inject]
    public void Init(IGameStateManager gameStateManager)
    {
        _gameStateManager = gameStateManager;
    }

    private void Awake()
    {
        Debug.Log("Loading started");
        _gameStateManager.Load();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Quitting application");
        _gameStateManager.Save();
    }

    private IGameStateManager _gameStateManager;
}
```

Скрипт Loader запускается самым первым в игре. Его я использую как отправную точку. А так же как скрипт, который следит за загрузкой и сохранением игрового состояния.

Теперь я накидаю простейший View для UI.

```csharp
public class CoinsView : MonoBehaviour
{
    public Text currencyText;

    [Inject]
    public void Init(IGameStateManager gameStateManager)
    {
        _gameStateManager = gameStateManager;
        UpdateView();
    }

    public void AddCoins()
    {
        _gameStateManager.GameState.coins += Random.Range(1,100);
        UpdateView();
    }

    public void RemoveCoins()
    {
        _gameStateManager.GameState.coins -= Random.Range(1,100);
        UpdateView();
    }

    public void UpdateView()
    {
        currencyText.text = "Coins: " + _gameStateManager.GameState.coins;
    }

    private IGameStateManager _gameStateManager;
}
```

Здесь я добавил два метода по добавлению и удалению произвольного количества монет. Стандартный подход, который я часто вижу в коде -- это пихать бизнес-логику прямо в UI.

Так не надо делать :). Но пока, давайте убедимся, что наш маленький прототип работает.

![UI Screenshot](https://github.com/PoisonousJohn/articles/raw/master/Telegram_Posts/decoupling-via-commands/images/ui-screenshot.png)

Кнопочки работают, состояние сохраняется и восстанавливается при загрузке.

Теперь давайте причешем наш код.

Сделаем отдельный тип команд, который модифицируют GameState.

```csharp
public interface ICommand
{

}
public interface IGameStateCommand : ICommand
{
    void Execute(GameState gameState);
}
```

Общий интерфейс сделаем пустым, чтобы обозначить единый тип комманд. Для команд, модифицирующих GameState, обозначим метод Execute, принимающий стейт в качестве параметра.

Создадим сервис, который будет запускать команды, модифицирующие стейт, типа того, который я показывал раньше. Интерфейс сделаем generic'ом, чтобы он подходил под любой тип комманд.

```csharp
public interface ICommandsExecutor<TCommand>
    where TCommand: ICommand
{
    void Execute(TCommand command);
}

public class GameStateCommandsExecutor : ICommandsExecutor<IGameStateCommand>
{

    public GameStateCommandsExecutor(IGameStateManager gameStateManager)
    {
        _gameStateManager = gameStateManager;
    }

    public void Execute(IGameStateCommand command)
    {
        command.Execute(_gameStateManager.GameState);
    }

    private readonly IGameStateManager _gameStateManager;
}
```

Регистрируем менеджер в DI.

```csharp
public class BindingsInstaller : MonoInstaller<BindingsInstaller>
{
    public override void InstallBindings()
    {
        Container.Bind<IGameStateManager>().To<LocalGameStateManager>().AsSingle();
        Container.Bind<Loader>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();

        // added this line
        Container.Bind<ICommandsExecutor<IGameStateCommand>>().To<GameStateCommandsExecutor>().AsSingle();
    }
}
```

Теперь сделаем реализацию самой команды.

```csharp
public class AddCoinsCommand : IGameStateCommand
{
    public AddCoinsCommand(int amount)
    {
        _amount = amount;
    }

    public void Execute(GameState gameState)
    {
        gameState.coins += _amount;
    }

    private int _amount;
}
```

Поменяем CoinsView, чтобы она использовала команды.

```csharp
public class CoinsView : MonoBehaviour
{
    public Text currencyText;

    [Inject]
    public void Init(IGameStateManager gameStateManager, ICommandsExecutor<IGameStateCommand> commandsExecutor)
    {
        _gameStateManager = gameStateManager;
        _commandsExecutor = commandsExecutor;
        UpdateView();
    }

    public void AddCoins()
    {
        var cmd = new AddCoinsCommand(Random.Range(1, 100));
        _commandsExecutor.Execute(cmd);
        UpdateView();
    }

    public void RemoveCoins()
    {
        var cmd = new AddCoinsCommand(-Random.Range(1, 100));
        _commandsExecutor.Execute(cmd);
        UpdateView();
    }

    public void UpdateView()
    {
        currencyText.text = "Coins: " + _gameStateManager.GameState.coins;
    }

    private IGameStateManager _gameStateManager;
    private ICommandsExecutor<IGameStateCommand> _commandsExecutor;
}
```

Теперь CoinsView использует GameState только для чтения. А все изменения стейта происходят посредством команд.

Что здесь портит картину -- так это вызов UpdateView вручную. Мы можем забыть его вызвать. Или состояние может обновиться посредством отправки команды из другого View.

Добавим событие об обновлении состояния в `ICommandExecutor`. Плюс сделаем отдельный интерфейс-алиас для Executor'a гейм стейт команд, чтобы скрыть лишние типы в дженерике.

```csharp
public interface ICommandsExecutor<TState, TCommand>
{
    // added event
    event System.Action<TState> stateUpdated;
    void Execute(TCommand command);
}
public interface IGameStateCommandsExecutor : ICommandsExecutor<GameState, IGameStateCommand>
{

}
```

Обновим регистрацию в DI

```csharp
public class BindingsInstaller : MonoInstaller<BindingsInstaller>
{
    public override void InstallBindings()
    {
        Container.Bind<IGameStateManager>().To<LocalGameStateManager>().AsSingle();
        Container.Bind<Loader>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
        // updated this line
        Container.Bind<IGameStateCommandsExecutor>()
                                            .To<DefaultCommandsExecutor>().AsSingle();
    }
}
```

Добавим событие в `DefaultCommandsExecutor`.

```csharp
public class DefaultCommandsExecutor : IGameStateCommandsExecutor
{
    // this event added
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

    public void Execute(IGameStateCommand command)
    {
        command.Execute(_gameStateManager.GameState);
        // these lines added
        if (_stateUpdate != null)
        {
            _stateUpdated(_gameStateManager.GameState);
        }
    }

    private readonly IGameStateManager _gameStateManager;
    // this line added
    private Action<GameState> _stateUpdated;

}
```

Стоит обратить внимание на реализацию ивента. Так как экзекутор шарит состояние только внутри ивента, важно его сразу дергать при подписке.

Теперь, наконец-то, обновим View.

```csharp
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
```

`IGameStateManager` теперь не нужен для View, так как UpdateView принимает GameState в качестве параметра. Отлично, избавились от лишней зависимости! Сам UpdateView мы подписываем на событие в `IGameStateCommandsExecutor`. Он будет вызываться при любом изменении состояния. Так же мы не забываем отписываться от события в OnDestroy.

Вот такой получился подход. Довольно чистый. Не замысловатый. Теперь невозможно забыть вызвать UpdateView в каком-то месте, при каком-то чертовом условии, которое воспроизводится только в определенную фазу луны.

Ну что-ж. Выдохнули, и идем дальше, там еще больше плюшек.

## Используем историю команд в качестве машины времени для отладки сложной логики

Как вы тестируете баги? Запускаем приложение, и следуем шагам по воспроизведению бага. Часто эти шаги выполняются вручную, ходим по UI, тыкаем кнопочки, все дела.

Все ничего, если баг простой, или условия воспроизведения бага легко повторить. Но что, если баг завязан на сетевую логику и время. К примеру, в игре есть какой-либо ивент, идущий в течение 10 минут. Баг возникает по завершению ивента.

Каждая итерация тестирования будет занимать **минимум** 10 минут. Обычно нужно несколько итераций, а между ними нужно что-то чинить.

Покажу интересный прием с использованием вышеописанного паттерна, который избавит вас от некоторой головной боли.

В коде из предыдущего пункта явно закрался баг. Ведь количество монет может быть отрицательным. Конечно, кейс далеко не самый сложный, но я надеюсь, у вас хорошее воображение.

Представьте, что логика сложная и баг трудоемко воспроизводить каждый раз. Но вот мы, или тестер на него случайно наткнулись. Что, если этот баг можно было бы "сохранить"?

> Теперь сам трюк: сохраним стейт, который был при запуске игры, а так же всю историю команд, совершенных над ним в течение игровой сессии.

Этих данных достаточно, чтобы воспроизводить баг столько раз, сколько необходимо, за доли секунды. При этом, даже нет необходимости запускать UI. Ведь все модификации поломанного стейта хранятся в истории. Это как небольшой интеграционный тест-кейс.

Переходим к реализации. Так как данное решение предполагает немного более продвинутую сериализацию, вроде сериализации интерфейсов, JsonUtility будет недостаточно. Поэтому я поставлю Json.Net for Unity из ассет стора.

Для начала, сделаем дебажную версию `IGameStateManager`, которая копирует "начальное" состояние игры в отдельный файл. То есть то состояние, что было на момент запуска игры.

```csharp
public class DebugGameStateManager : LocalGameStateManager
{
    public override void Load()
    {
        base.Load();
        File.WriteAllText(BACKUP_GAMESTATE_PATH, JsonUtility.ToJson(GameState));
    }

    public void SaveBackupAs(string name)
    {
        File.Copy(
            Path.Combine(Application.persistentDataPath, "gameStateBackup.json"),
            Path.Combine(Application.persistentDataPath, name + ".json"), true);
    }

    public void RestoreBackupState(string name)
    {
        var path = Path.Combine(Application.persistentDataPath, name + ".json");
        Debug.Log("Restoring state from " + path);
        GameState = JsonUtility.FromJson<GameState>(File.ReadAllText(path));
    }

    private static readonly string BACKUP_GAMESTATE_PATH
                            = Path.Combine(Application.persistentDataPath, "gameStateBackup.json");

}
```

За кадром я оставил преобразование методов родительского класса в виртуальные. Оставлю это вам как упражнение. Ко всему прочему добавлен метод `SaveBackupAs`, который понадобится в дальнейшем, чтобы мы могли сохранять наши "слепки" с определенным именем.

Теперь создадим дебажную версию экзекутора, который умеет хранить историю команд, да и вообще сохраняет полный слепок "начлаьное состояние+команды".

```csharp
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
```

Здесь как раз видно, что стандартных возможностей JsonUtility не хватило бы. Мне пришлось задать `TypeNameHandling` для настроек сериализации, чтобы при загрузке/сохранении слепка команды десериализовались именно в типизированные объекты, ведь к ним привязана логика.

Что еще примечательного в этом экзекуторе?

* Сохраняет каждую команду в историю
* Умеет сохрнять и восстанавливать историю команд и стейт игры
* Ключевой метод Replay "прогирывает" все команды, начиная с изначального состояния игры, и до команды с указанным индексом

Я бы не хотел, чтобы в релизном проекте, история забивала память, поэтому я сделаю регистрацию данного сервиса в DI только при наличии DEBUG дефайна.

```csharp
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
```

Ах да, нужно подготовить команду к сериализации:

```csharp
public class AddCoinsCommand : IGameStateCommand
{

    public AddCoinsCommand(int amount)
    {
        _amount = amount;
    }

    public void Execute(GameState gameState)
    {
        gameState.coins += _amount;
    }

    public override string ToString() {
        return GetType().ToString() + " " + _amount;
    }

    [JsonProperty("amount")]
    private int _amount;
}
```

Здесь я добавил JsonProperty, так как свойство приватное. Так же я добавил ToString(), чтобы красиво выводить команду в дальнейшем.

Чтобы заработала дебажная версия, не забудьте добавить "DEBUG" в Player Settings -> Other Settings -> Scripting define symbols.

Далее я хочу иметь возможность сохранять/загружать историю команд и состояние прямо из интерфейса Unity. Намутим отдельный EditorWindow.

```csharp
public class CommandsHistoryWindow : EditorWindow
{

    [MenuItem("Window/CommandsHistoryWindow")]
    public static CommandsHistoryWindow GetOrCreateWindow()
    {
        var window = EditorWindow.GetWindow<CommandsHistoryWindow>();
        window.titleContent = new GUIContent("CommandsHistoryWindow");
        return window;
    }

    public void OnGUI()
    {

        // this part is required to get
        // DI context of the scene
        var sceneContext = GameObject.FindObjectOfType<SceneContext>();
        if (sceneContext == null || sceneContext.Container == null)
        {
            return;
        }
        // this guard ensures that OnGUI runs only when IGameStateCommandExecutor exists
        // in other words only in runtime
        var executor = sceneContext.Container.TryResolve<IGameStateCommandsExecutor>() as DebugCommandsExecutor;
        if (executor == null)
        {
            return;
        }

        // general buttons to load and save "snapshot"
        EditorGUILayout.BeginHorizontal();
        _replayName = EditorGUILayout.TextField("Replay name", _replayName);
        if (GUILayout.Button("Save"))
        {
            executor.SaveReplay(_replayName);
        }
        if (GUILayout.Button("Load"))
        {
            executor.LoadReplay(_replayName);
        }
        EditorGUILayout.EndHorizontal();

        // and the main block which allows us to walk through commands step by step
        EditorGUILayout.LabelField("Commands: " + executor.commandsHistory.Count);
        for (int i = 0; i < executor.commandsHistory.Count; ++i)
        {
            var cmd = executor.commandsHistory[i];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(cmd.ToString());
            if (GUILayout.Button("Step to"))
            {
                executor.Replay(_replayName, i + 1);
            }
            EditorGUILayout.EndHorizontal();

        }
    }

    private string _replayName;
}
```

Получилось довольно простенько. Теперь как это выглядит?

![Animated GIF of commandHistoryWindow](https://github.com/PoisonousJohn/articles/raw/master/Telegram_Posts/decoupling-via-commands/images/commandHistoryWindow.gif)

Я сразу сохранил пустой "initial" стейт, чтобы, если что к нему вернуться.
Далее я натыкал пару раза кнопками, счетчик монет поменялся, а так же мы видим список команд, примененных к стейту.

Затем я сохранил полученный слепок под именем version1.

Далее я использую кнопки Step to, чтобы "проиграть" изменения по новой, до определенной команды.

Теперь вернемся к багу с отрицательным значением монет. Допустим тестер случайно наткнулся на баг. Я сделал кнопку "сохранить снапшот" только в юнити, но это можно вынести и в интерфейс игры. В данном случае, тестер может указать имя снапшота "negativeCoins" и тыкнуть на кнопку save.

Дальше он может залезть в папку с сохранениями, и найти два файла negativeCoins.json и negativeCoins_commands.json, и кинуть их разработчику. Разработчик кладет их к себе в папку с сохранками, пишет то же название negativeCoins, тыкает Load и вуаля. У нас на руках готовый тест кейс.

Более того, можно сделать пустую сцену, без UI, на которой можно только проигрывать снапшоты и смотреть на стейт. И это может сэкономить кучу времени.

Вокруг этого функционала даже можно построить процесс интеграционного тестирования. Например, хранить список проблемных "слепков", который нужно тестировать при каждой пересборке билда, и следить, чтобы ничего не отломалось.

Ну да ладно фантазировать, пофиксим баг уже.

```csharp
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
```

И проверим фикс на слепке `version1`, который я сохранил в прошлый раз.

![Animated GIF of fixed bug replayed with CommandsHistoryWindow](https://github.com/PoisonousJohn/articles/raw/master/Telegram_Posts/decoupling-via-commands/images/fixedBug.gif)

Как мы видим, монеты больше не уходят в минус. Победа!

## Подводим итоги

В статье я рассказал свое видение паттерна Command. Я считаю что у него очень много применени. Я показал всего лишь несколько из тех, что я использую.

Так же я затронул больную тему UI, подход Flux, а так же реактивный подход в UI.

Я показал интересный способ отладки, похожий на машину времени, когда можно, буквально, проигрывать изменения стейта, как в отладчике.

Скомпоновав эти паттерны вместе, получилась довольная гибкая штука, которую должно быть удобно поддерживать, рефакторить, дебажить. Конечно, много чего еще можно улучшить/доработать. Но это уже на ваше усмотрение =).

Что я хочу еще отметить. Я считаю, что в данном случае, реактивность UI, а так же использование команд, сильно развязали руки. Ведь, когда я добавил дебажные версии экзекутора и GameStateManager'a, в UI я абсолютно ничего не менял.

Исходный код вы можете найти в [репозитории](https://github.com/PoisonousJohn/articles/tree/master/Telegram_Posts/decoupling-via-commands/Patterns).

Построение UI -- это довольно обширная тема, и этому будет посвящена отдельная статья.

Если вам понравилась эта статья, ставьте лайк =), пишите комменты.

Подписывайтесь на меня, чтобы не пропустить новые статьи:

* [Мой канал GameDev Architecture](https://t.me/gamedev_architecture)
* [Я в Telegram](https://t.me/poisonous_john)
* [Я на Github](https://github.com/poisonousjohn)
* [Мой Twitter](https://twitter.com/poisonous_john)