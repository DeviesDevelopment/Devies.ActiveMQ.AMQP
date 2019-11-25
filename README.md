# Devies.ActiveMQ.AMQP


## Requriments
- An ActiveMQ istance or some other Broker with AMQP 1.0 available

## How to

1. Install the nuget package `dotnet add package Devies.ActiveMQ.AMQP`
2. Create your `QueueOptions`
3. Create your queue message
4. Create your task
5. Call services.AddQueueReader to start consuming your queue
6. `AddQueueReader` will add a HostedService to consume the queue in the background

## Example Consuimg
#### appsettings.Development.json
```json
{
   "Settings": {
      "Url": "amqp.org",
      "Port": "5672",
      "User": "username",
      "Pass": "password",
      "ReceiverName": "Service",
      "Queue": "APP.TEST.QUEUE.TEST"
    }
}
```

#### QueueMessage.cs
```c#
public class QueueMessage
{
    public string Identifier { get; set; }
}
```


#### QueueTask.cs
```c#
public class QueueTask : BaseQueueTask<QueueMessage>
{
    public QueueTask(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    public override async Task<MessageAction> Execute(IServiceProvider serviceProvider,
        QueueMessage message)
    {
        Console.WriteLine(message.Identifier);

        return MessageAction.ACCEPT;
    }
}
```

#### Startup.cs

```c#
public void ConfigureServices(IServiceCollection services)
{
    var queueOptions = Configuration
        .GetSection("QueueOptions")
        .Get<QueueOptions>();

    ...

    services.AddQueueReader<QueueMessage, QueueTask>(queueOptions));
}
```

## Example posting
#### Startup.cs
```c#
public void ConfigureServices(IServiceCollection services)
{
    var queueOptions = Configuration
        .GetSection("QueueOptions")
        .Get<QueueOptions>();

    ...

    services.AddSingleton<IQueueWriteService<QueueMessage>>(s =>
        new WriteAMPQMQService<QueueMessage>(queueOptions));
}
```

```c# HomeController.cs
public class HomeController : Controller
{
    private readonly IQueueWriteService<QueueMessage> queueWriter;

    public HomeController(IQueueWriteService<QueueMessage> queueWriter)
    {
        this.queueWriter = queueWriter;
    }

    [HttpGet("test")]
    public async Task<IActionResult> Test()
    {
        await queueWriter.AddToQueue(new List<QueueMessage> { 
            new QueueMessage { Identifier = "123" }
        });
    }
}
    

```


## Dependencies
- https://www.nuget.org/packages/AMQPNetLite.Core/



