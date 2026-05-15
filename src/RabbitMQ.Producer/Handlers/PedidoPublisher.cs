using RabbitMQ.Client;
using RabbitMQ.Models.Models.Pedido;
using RabbitMQ.Shared.Messaging;
using System.Text.Json;

namespace RabbitMQ.Producer.Handlers
{
    public class PedidoPublisher(IChannel channel)
    {
        public async Task PublicarAsync(PedidoRequest pedido)
        {
            var body = JsonSerializer.SerializeToUtf8Bytes(pedido);

            var properties = new BasicProperties
            {
                DeliveryMode = DeliveryModes.Persistent,
                ContentType = "application/json",
                ContentEncoding = "utf-8"
            };

            await channel.BasicPublishAsync(
                exchange: Exchanges.Principal,
                routingKey: RoutingKeys.PedidoCriado,
                mandatory: false,
                basicProperties: properties,
                body: body);

            Console.WriteLine($"✅ Pedido {pedido.Id} enviado!");
        }
    }
}
