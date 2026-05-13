using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Models;
using System.Text.Json;

namespace RabbitMQ.Application.Services.Handlers
{
    public class PedidoHandler
    {
        private readonly ILogger<PedidoHandler> _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly IHubContext<PedidoHub> _hub;
        private IChannel? _channel;

        public PedidoHandler(ILogger<PedidoHandler> logger, IHubContext<PedidoHub> hub)
        {
            _logger = logger;
            _hub = hub;
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
            try
            {
                var json = System.Text.Encoding.UTF8.GetString(eventArgs.Body.ToArray());

                var evento = JsonSerializer.Deserialize<PedidoProcessadoEvento>(json);

                if (evento is null)
                {
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: false);
                    return;
                }

                _logger.LogInformation("Evento recebido: Pedido {Id} — {Status}", evento.PedidoId, evento.Status);

                // Empurra para todos os clientes conectados via SignalR
                await _hub.Clients.All.SendAsync("NovoPedido", evento, eventArgs.CancellationToken);

                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento");

                await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: false);
            }
        }
    }
}
