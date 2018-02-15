TODO:
* [ ] Описать создание EventHub

# Строим свою игровую аналитику в облаке: отправляем и сохраняем данные

В предыдущей части я рассказал почему необходима своя игровая аналитика, как ее построить и сколько это будет стоить. В этой статье я подробно опишу детали технического решения: как посылать данные в Azure EventHub, как их сохранять в Azure Data Lake Store и как правильно их разложить в хранилище для оптимизации запросов.

## Создание EventHub

## Посылка событий

Начнем с EventHub -- штука довольно простая. Имеет определенную пропускную способность. За нее и платишь. 1 Troughput Unit (TU) позволяет обрабатывать:

- на вход **до 1 MB/сек или 1000 ивентов/сек (что наступит быстрее)**.
- на выход до 2MB/сек

При превышении лимита на входящие данные, сервис отфутболивает реквест и кидает ServiceBusyException. Превышение лимитов на исходящие данные эксепшена не кидает, но скорость ограничивается. Максимальный размер события -- 64Кб.
Поэтому есть выбор, либо слать более жирные ивенты, но реже, либо мелкие, но чаще. Как правило, первый вариант предпочтительнее. Но я начал со второго, так как он проще.

Сервис может принимать сообщения по двум протоколам:

- AMQP
- HTTP

AMQP наиболее эффективный способ, но количество бесплатных коннектов по данному протоколу сильно ограничено. Поэтому AMQP подойдет, если посылать в EventHub серверную аналитику. Клиентскую аналитику я решил посылать напрямую в EventHub, поэтому HTTP. Писать тестовый клиент я решил на .Net Core. К сожалению, на момент написания статьи, официальный EventHub SDK не поддерживал HTTP протокол.

Поэтому я быстро накидал простенький HTTP клиент, для начала. Endpoint для отправки формируется следующим образом:

`_restAPIUrl = $"https://{eventHubNamespace}.servicebus.windows.net/{entityPath}";`

Где eventHubNamespace -- доменное имя Event Hub'a, которое вы указали при создании. А entityPath -- имя конкретного EventHub'a, куда будут слаться события.

Немного пришлось повозиться с генерацией SAS токена, необходимого для отправки ивента.

```csharp
	private string CreateToken(string url)
	{
		TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);
		var week = 60 * 60 * 24 * 7;
		var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + week);
		string stringToSign = HttpUtility.UrlEncode(url) + "\n" + expiry;
		HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_key));
		var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
		var sasToken = String.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(url), HttpUtility.UrlEncode(signature), expiry, _keyName);
		return sasToken;
	}
```

Данный код не следует размещать на клиенте, так как генерация токена требует приватного ключа. В идеале, клиент должен использовать токен, сгенерированный безопасным образом на сервере.

Отправка запроса -- попроще. Все что нужно -- добавить токен в заголовок Authorization.

```csharp
	var msg = new HttpRequestMessage(HttpMethod.Post, _restApiUrl);
	msg.Content = new StringContent(@event);
	msg.Headers.Add("Authorization", _token);

	var resp = await _httpClient.SendAsync(msg);
```

После тестового запуска клиента, можно зайти в портал и посмотреть, есть ли всплеск на графике входящих событий.

!! Вставить картинку со всплеском графика !!

## Создаем приложение Azure Functions

Создадим сначала само приложение Azure Functions.
Для этого, в портале тыкаем Create Resource, в поиск вводим "Function app".
Не особо оевидно, так как я сначала искал "Azure Functions" и искренне удивлялся отсутствию подходящих результатов.

![Create-func-app](custom-analytics-images/create-func-app.png)

Важный аспект -- создать новый App Service Plan и выбрать Pricing tier. Начать можно с бесплатного, а потом, после тестов, переключиться на что-нибудь побыстрее.

Дальше переходим к самому приложению. Чтобы извлечь максимальную производительность из Azure Functions, нужно использовать скомпилированное C# приложение. Для этого проще всего воспользоваться шаблоном приложения Azure Functions в Visual Studio. Этот шаблон будет доступен после [установки поддержки Azure Functions для VS](https://docs.microsoft.com/ru-ru/azure/azure-functions/functions-develop-vs).

Создаем новый проект Azure Functions в Visual Studio. Выбираем шаблон Queue Trigger. Path -- это имя конкретного EventHub'a, созданного ранее. Connection можно пока пропустить.

После этого откроется шаблон примерно такого вида:

```csharp
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace FunctionApp
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static void Run([QueueTrigger("myqueue-items", Connection = "")]string myQueueItem, TraceWriter log)
        {
            log.Info($"C# Queue trigger function processed: {myQueueItem}");
        }
    }
}
```

Интеграция с EventHub'ом настраивается посредством аттрибута QueueTrigger. Чтобы все заработало, необходимо заполнить Connection. Там нужно указать имя настройки приложения функций (App Setting), в котором будет Connection String к Event Hub.

При локальной разработке App Settings задаются в файле local.settings.json. Саму Connection String можно взять в настройках Event Hub'a в портале, в разделе Shared access policies. По умолчанию там только одна политика **RootManageSharedAccessKey**.

Для нашей функции нужен доступ только на чтение, поэтому я создам новую политику ADLSOutputterKey, и дам права только Listen права. Данную политику можно создать как в корне EventHub'a, так и для конкретной сущности (пути), тогда его область видимости будет еще более ограничена.

Теперь, кликнув по новому ключу, можно скопировать Connection String. Открываем local.settings.json и добавляем строку в настройки.

```json
{
  "IsEncrypted": false,
  "Values": {
    "EventHub": "<your-eventhub-connection-string>",
    "AzureWebJobsStorage": "",
    "AzureWebJobsDashboard": "",
  },
}
```

Чтобы можно было дебажить Azure Functions локально, нужно еще несколько настроек. Они есть внутри приложения Azure Functions, созданном в портале.

Открываем Azure Functions в портале, и тыкаем по Application Settings, там находим AzureWebJobsStorage, AzureWebJobsDashboard, копируем их значения и переносим в local.settings.json.

После этого пробуем собрать и запустить приложение в Visual Studio. Вы должны увидеть открывшуюся консоль, с запущенным Azure Functions хостом, оповещающем о том что он готов к работе.

Посылаем несколько сообщений в EventHub. Если настройка выполнена правильно, то в логе появятся сообщения об обработанных сообщениях.

## Авторизуемся в Azure Data Lake Store

## Записываем данные в хранилище

## Оптимизируем количество запросов

