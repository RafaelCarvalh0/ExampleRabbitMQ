using RabbitMQ.Client;
using RabbitMQ.Shared.Messaging;

namespace RabbitMQ.Shared.Infrastructure
{
    public static class RabbitMqQueueSetup
    {
        public static async Task ConfigureAsync(IChannel channel)
        {
            #region Exchange Principal + Principal Queue Configuration

            // Principal
            await channel.ExchangeDeclareAsync(exchange: Exchanges.Principal, type: ExchangeType.Direct, durable: true, autoDelete: false);

            // Fila principal com DLX configurado
            var mainQueueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", Exchanges.Dlx },
                { "x-dead-letter-routing-key", RoutingKeys.PedidoFalha }
            };

            // Fila principal
            await channel.QueueDeclareAsync(queue: Queues.Principal, durable: true, exclusive: false, autoDelete: false, arguments: mainQueueArgs!);

            // Bind fila principal ao exchange principal
            await channel.QueueBindAsync(queue: Queues.Principal, exchange: Exchanges.Principal, routingKey: RoutingKeys.PedidoCriado);

            #endregion


            #region Exchange + DLQ Queue Configuration

            // DLX
            await channel.ExchangeDeclareAsync(exchange: Exchanges.Dlx, type: ExchangeType.Direct, durable: true, autoDelete: false);

            // DLQ
            await channel.QueueDeclareAsync(queue: Queues.Dlq, durable: true, exclusive: false, autoDelete: false);

            // Bind DLQ ao exchange de DLX
            await channel.QueueBindAsync(queue: Queues.Dlq, exchange: Exchanges.Dlx, routingKey: RoutingKeys.PedidoFalha);

            #endregion


            #region Exchange + Retry Queue Configuration

            // Retry
            await channel.ExchangeDeclareAsync(exchange: Exchanges.Retry, type: ExchangeType.Direct, durable: true, autoDelete: false);

            // Retry Queue
            var retryQueueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", Exchanges.Principal },
                { "x-dead-letter-routing-key", RoutingKeys.PedidoCriado },
                { "x-message-ttl", Retries.RetryDelayMs }
            };

            // Fila de retry
            await channel.QueueDeclareAsync(queue: Queues.Retry, durable: true, exclusive: false, autoDelete: false, arguments: retryQueueArgs!);

            // Bind fila de retry ao exchange de retry
            await channel.QueueBindAsync(queue: Queues.Retry, exchange: Exchanges.Retry, routingKey: RoutingKeys.PedidoRetry);

            #endregion


            #region Exchange Processado + Processado Queue Configuration

            // Exchange para pedidos processados                                     // Fanout — todos os consumers recebem
            await channel.ExchangeDeclareAsync(exchange: Exchanges.Processado, type: ExchangeType.Fanout, durable: true, autoDelete: false);

            // Fila para a aplicação
            await channel.QueueDeclareAsync(queue: Queues.Processado, durable: true, exclusive: false, autoDelete: false);
                                                                                                   // Fanout ignora routing key                                                                  
            await channel.QueueBindAsync(queue: Queues.Processado, exchange: Exchanges.Processado, routingKey: string.Empty); 

            #endregion
        }
    }
}
