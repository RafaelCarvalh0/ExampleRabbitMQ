using RabbitMQ.Client;
using RabbitMQ.Shared.Messaging;

namespace RabbitMQ.Shared.Infrastructure
{
    public static class RabbitMqQueueSetup
    {
        public static async Task ConfigureAsync(IChannel channel)
        {
            // Exchange principal
            await channel.ExchangeDeclareAsync(
                exchange: Exchanges.Principal,
                type: ExchangeType.Direct, // Direct, Fanout, Topic, Headers
                durable: true,
                autoDelete: false);

            // DLX
            await channel.ExchangeDeclareAsync(
                exchange: Exchanges.Dlx,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // DLQ
            await channel.QueueDeclareAsync(
                queue: Queues.Dlq,
                durable: true, // Persistente, sobrevive a reinícios do RabbitMQ
                exclusive: false,
                autoDelete: false);

            // Bind DLQ à DLX
            await channel.QueueBindAsync(
                queue: Queues.Dlq,
                exchange: Exchanges.Dlx,
                routingKey: RoutingKeys.PedidoFalha);

            // Fila principal com DLX configurado
            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", Exchanges.Dlx },
                { "x-dead-letter-routing-key", RoutingKeys.PedidoFalha }
            };

            // Fila principal
            await channel.QueueDeclareAsync(
                queue: Queues.Principal,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args);

            // Bind fila principal ao exchange principal
            await channel.QueueBindAsync(
                queue: Queues.Principal,
                exchange: Exchanges.Principal,
                routingKey: RoutingKeys.PedidoCriado);
        }
    }
}
