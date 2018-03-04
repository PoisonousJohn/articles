# Развязываем игровой код c помощью паттерна "Command"

В [предыдущей статье](http://goo.gl/NP9Yfy) я поднимал вопрос развязывания кода. И рассмотрел несколько вариантов: интерфейсы, Dependency Injection.

В этой статье я хочу разобрать еще один паттерн: [Команда (Command)](https://ru.wikipedia.org/wiki/%D0%9A%D0%BE%D0%BC%D0%B0%D0%BD%D0%B4%D0%B0_(%D1%88%D0%B0%D0%B1%D0%BB%D0%BE%D0%BD_%D0%BF%D1%80%D0%BE%D0%B5%D0%BA%D1%82%D0%B8%D1%80%D0%BE%D0%B2%D0%B0%D0%BD%D0%B8%D1%8F)). Он поможет избежать лишних зависимостей и упростить сложные алгоритмы. Это один из моих самых любимых паттернов.

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

А как же их исполнять? Напишем простую систему обработки команд.

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
* Сложную бизнес логику легко выражать посредством набора простейших команд. Команды легко переиспользовать.
* Команда выступает может выступать чекпойнтом, ну или транзакцией, как вам больше нравится. Если изменение состояния данных происходит только посредством команд, это упрощает отладку, да и понимание программы. Если что-то сломалось, вы всегда можете проследить какая команда привела к ошибке. Что удобно -- можно видеть и параметры, с которыми была выполнена команда.
* Выполнение команд может быть отложенным. Типичный пример -- отправка команды на сервер. Когда пользователь инициировал какое-либо действие в игре, создается команда, и добавляется в очередь на исполнение. Фактическое же исполнение команды происходит только после подтверждения от сервера.
* Код, написанный с идеологией команд, немного отличается от традиционного подхода с вызовом функций. Когда программист создает команду, он сообщает о необходимости изменить состояние. Как и когда это будет сделано -- его не интересует. Это позволяет творить интересные вещи.

Немного подробнее про последний пункт. Например, у вас была синхронная функция, которая должна стать асинхронной. Чтобы ее сделать это, вам необходимо изменить ее сигнатуру, и написать механизм обработки асинхронного результата в виде коллбека, или корутины, или async/await (если вы переползли на .net 4.6). И так каждый раз, для каждой отдельно взятой функции.

Механизм команд позволяет абстрагироваться от механизма исполнения. Поэтому если команда раньше исполнялась моментально, ее легко можно сделать асинхронной. Это даже можно менять динамичеки, в рантайме.

Конкретный пример. Игра поддерживает частичный оффлайн. Если сейчас сетевое соединение недоступно, то команды попадают в очередь, и исполняются в момент восстановления соединения. Если соединение есть, то команды исполняются моментально.

## Используем команды для отладки сложной логики

Как вы тестируете баги? Запускаем приложение, и следуем шагам по воспроизведению бага. Часто эти шаги выполняются вручную, ходим по UI, тыкаем кнопочки, все дела.

Все ничего, если баг простой, или условия воспроизведения бага легко повторить. Но что, если баг завязан на сетевую логику и время. К примеру, в игре есть какой-либо ивент, идущий в течение 10 минут. Баг возникает по завершению ивента.

Каждая итерация тестирования будет занимать **минимум** 10 минут. Обычно нужно несколько итераций, а между ними нужно что-то чинить.

Покажу интересный прием с использованием паттерна команд, который избавит вас от некоторой головной боли.

Для начала опредилимся с состоянием игры. Пусть это будет следующая структура данных:

```csharp
    public struct GameState
    {
        public int Coins { get; set; }
    }
```

Добавим сохранение состояния игры в файл формата JSON.

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

В [предыдущей статье](https://medium.com/p/%D0%B0%D0%BD%D1%82%D0%B8%D0%BF%D0%B0%D1%82%D1%82%D0%B5%D1%80%D0%BD%D1%8B-%D0%B2-%D0%B8%D0%B3%D1%80%D0%BE%D0%B2%D0%BE%D0%B9-%D1%80%D0%B0%D0%B7%D1%80%D0%B0%D0%B1%D0%BE%D1%82%D0%BA%D0%B5-%D0%B7%D0%B0%D0%B2%D0%B8%D1%81%D0%B8%D0%BC%D0%BE%D1%81%D1%82%D0%B8-%D0%B2-%D0%BA%D0%BE%D0%B4%D0%B5-1bd879ef46ad) я рассматривал пример того, как можно вынести зависимости в отдельный класс, если нет возможности использовать Dependency Injection. Сейчас я вынесу в этот класс определение IGameStateManager.

```csharp
using System;

namespace Patterns
{

    public static class Services
    {
        public static IGameStateManager gameStateManager { get { return _gameStateManager.Value; } }
        public static Lazy<IGameStateManager> _gameStateManager = new Lazy<IGameStateManager>(() =>
        {
            return new LocalGameStateManager();
        });

        public static Func<string, ILogger> loggerFabric = (name) =>
        {
            return new DefaultUnityLogger(name);
        };

        public static ILogger log { get { return _log.Value; } }
        public static Lazy<ILogger> _log = new Lazy<ILogger>(() =>
        {
            return loggerFabric("General");
        });

        public static Lazy<IAPIService> api = new Lazy<IAPIService>(
        () =>
        {
            return new APIService(_log);
        });
    }

}
```

Заодно я сделал отдельный класс логгера, чтобы удобно именовать логгеры, и указывать таймштамп записи в логе. Я добавил лог по умолванию `Services.log`, а так же фабрику для создания именованного логгера `Services.loggerFabric`.

```csharp
public interface ILogger
{
    void Log(string msg);
    void Warning(string msg);
    void Error(string msg);

}

public class DefaultUnityLogger : ILogger
{
    public DefaultUnityLogger(string name)
    {
        _name = name;
    }

    public void Error(string msg)
    {
        Debug.LogError(Format(msg));
    }

    public void Log(string msg)
    {
        Debug.Log(Format(msg));
    }

    public void Warning(string msg)
    {
        Debug.LogWarning(Format(msg));
    }

    private string Format(string msg)
    {
        return string.Format("[{0}]<{1}>: {2}", DateTime.Now, _name, msg);
    }

    private readonly string _name;
}
```

Теперь я добавлю загрузку и сохранение игрового состояния при запуске и выходе из игры соответственно.

```csharp
public class Loader : MonoBehaviour {

    private void Awake() {
        _log = Services.loggerFabric("Loader");
        _log.Log("Loading started");
        Services.gameStateManager.Load();
    }

    private void OnApplicationQuit()
    {
        _log.Log("Quitting application");
        Services.gameStateManager.Save();
    }

    private ILogger _log;
}
```

Скрипт Loader запускается самым первым в игре. Его я использую как отправную точку.