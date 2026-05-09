using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Consumer.Handlers;
using RabbitMQ.Shared.Infrastructure;
using RabbitMQ.Shared.Messaging;

var settings = new RabbitMqSettings();
await using var connection = await RabbitMqConnectionFactory.CreateAsync(settings);
await using var channel = await connection.CreateChannelAsync();

await RabbitMqQueueSetup.ConfigureAsync(channel);
await channel.BasicQosAsync(0, prefetchCount: 1, global: false);

var handler = new PedidoHandler(channel);
var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += handler.HandleAsync;

await channel.BasicConsumeAsync(Queues.Principal, autoAck: false, consumer);

Console.WriteLine("🚀 Consumer rodando. ENTER para parar...");
Console.ReadLine();