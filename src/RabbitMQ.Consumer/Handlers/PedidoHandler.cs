using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Models.Models;
using RabbitMQ.Shared.Messaging;
using System.Text.Json;
using RabbitMQ.Models;
using RabbitMQ.Infrasctructure.Repositories;

public class PedidoHandler
{
    private readonly ILogger<PedidoHandler> _logger;
    private readonly IPedidoRepository _repository;
    private IChannel? _channel;
    private const bool SimularErroTemporario = false;

    // ← Garante processamento sequencial
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public PedidoHandler(ILogger<PedidoHandler> logger, IPedidoRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public void SetChannel(IChannel channel) => _channel = channel;

    public async Task HandleAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        await _semaphore.WaitAsync();
        try
        {
            await ProcessarAsync(eventArgs);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ProcessarAsync(BasicDeliverEventArgs eventArgs)
    {
        int retryCount = 0;

        var json = System.Text.Encoding.UTF8.GetString(eventArgs.Body.ToArray());

        var pedido = JsonSerializer.Deserialize<Pedido>(json);

        try
        {
            if (eventArgs.BasicProperties.Headers?.TryGetValue("x-retry-count", out var retryCountObj) == true)
            {
                retryCount = Convert.ToInt32(retryCountObj);
            }

            _logger.LogInformation("Tentativa {Attempt}/{Max}", retryCount + 1, Retries.MaxRetryAttempts);

            #region Validações de negócio

            if (pedido is null)
                throw new Exception("Pedido inválido — desserialização retornou null");

            if (pedido.ValorTotal < 0)
            {
                _logger.LogWarning("Pedido {Id} com valor negativo → DLQ", pedido.Id);

                await PublicarEventoAsync(pedido, "Falhou", "Valor negativo");
                await _channel!.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: false);
                return;
            }

            if (string.IsNullOrWhiteSpace(pedido.ClienteEmail))
            {
                _logger.LogWarning("Pedido {Id} sem e-mail → DLQ", pedido.Id);

                await PublicarEventoAsync(pedido, "Falhou", "Email inválido");
                await _channel!.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: false);
                return;
            }

            if (await _repository.ExistsAsync(pedido.Id))
            {
                _logger.LogWarning("Pedido {Id} já processado — duplicata ignorada", pedido.Id);

                await _channel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                return;
            }

            if (SimularErroTemporario)
                throw new Exception("Erro temporário simulado!");

            #endregion

            await PublicarEventoAsync(pedido, "Processado", null, retryCount + 1);
            _logger.LogInformation("Pedido {Id} persistido no MongoDB com sucesso", pedido.Id);

            await _channel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON corrompido → DLQ");
            await _channel!.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro → avaliando retry");
            await HandleRetryAsync(eventArgs, pedido, retryCount);
        }
    }

    private async Task PublicarEventoAsync(Pedido pedido, string status, string? motivo = null, int? tentativas = null)
    {
        PedidoProcessado pedidoProcessado = PedidoProcessado.FromPedido(pedido, status, motivo, tentativas);
        await _repository.SaveAsync(pedidoProcessado);

        var json = JsonSerializer.Serialize(pedidoProcessado);
        var body = System.Text.Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            ContentType = "application/json"
        };

        await _channel!.BasicPublishAsync(
            exchange: Exchanges.Processado,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: props,
            body: body);
    }

    private async Task HandleRetryAsync(BasicDeliverEventArgs eventArgs, Pedido pedido, int retryCount)
    {
        if (retryCount < Retries.MaxRetryAttempts - 1)
        {
            var newRetryCount = retryCount + 1;

            var retryProperties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                ContentType = "application/json",
                ContentEncoding = "utf-8",
                Headers = new Dictionary<string, object?>
                {
                    { "x-retry-count", newRetryCount }
                }
            };

            await _channel!.BasicPublishAsync(
                exchange: Exchanges.Retry,
                routingKey: RoutingKeys.PedidoRetry,
                mandatory: false,
                basicProperties: retryProperties,
                body: eventArgs.Body.ToArray());

            _logger.LogInformation("Retry {Next}/{Max} em {Delay}ms", newRetryCount + 1, Retries.MaxRetryAttempts, Retries.RetryDelayMs);

            await _channel!.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
        }
        else
        {
            _logger.LogError("Limite de retries excedido → DLQ");

            await PublicarEventoAsync(pedido, "Falhou", "Limite de retries excedido", retryCount);
            await _channel!.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
        }
    }
}