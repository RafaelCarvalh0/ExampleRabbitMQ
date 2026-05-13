using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Shared.Infrastructure;
using RabbitMQ.Shared.Messaging;

namespace RabbitMQ.Consumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMqSettings _settings;
    private readonly PedidoHandler _handler;
    private IConnection? _connection;
    private IChannel? _channel;

    public Worker(
        ILogger<Worker> logger,
        RabbitMqSettings settings,
        PedidoHandler handler)
    {
        _logger = logger;
        _settings = settings;
        _handler = handler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker Consumer iniciando...");

        _connection = await RabbitMqConnectionFactory.CreateConnectionAsync(_settings);
        _channel = await _connection.CreateChannelAsync();

        await RabbitMqQueueSetup.ConfigureAsync(_channel);
        await _channel.BasicQosAsync(0, prefetchCount: 1, global: false);

        // Injeta o channel no handler agora que ele existe
        _handler.SetChannel(_channel);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += _handler.HandleAsync;

        await _channel.BasicConsumeAsync(
            queue: Queues.Principal,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "Aguardando mensagens na fila '{Queue}'...", Queues.Principal);

        // Mantém vivo até o host pedir parada
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Consumer encerrando...");

        if (_channel is { IsOpen: true })
            await _channel.CloseAsync();

        if (_connection is { IsOpen: true })
            await _connection.CloseAsync();

        await base.StopAsync(cancellationToken);
    }
}