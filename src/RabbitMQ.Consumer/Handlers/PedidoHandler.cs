using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Model.Models;
using RabbitMQ.Shared.Messaging;
using System.Text.Json;

namespace RabbitMQ.Consumer.Handlers
{
    public class PedidoHandler(IChannel channel)
    {
        private int retryCount = 0;
        private const bool simularErroTemporario = false;

        public async Task HandleAsync(object sender, BasicDeliverEventArgs eventArgs)
        {
            try
            {
                if (eventArgs.BasicProperties.Headers != null && eventArgs.BasicProperties.Headers.TryGetValue("x-retry-count", out var retryCountObj))
                {
                    retryCount = Convert.ToInt32(retryCountObj);
                }
                Console.WriteLine($"🔄 Tentativa de retry: {retryCount + 1}/{Retries.MaxRetryAttempts}");

                var json = System.Text.Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var pedido = JsonSerializer.Deserialize<Pedido>(json);

                if (pedido is null)
                    throw new Exception("Pedido inválido!");

                if (pedido.ValorTotal < 0)
                {
                    Console.WriteLine($"❌ Produto com valor negativo: {pedido.ValorTotal:C} → DLQ");
                    await channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: false);
                    return;
                }

                if (string.IsNullOrWhiteSpace(pedido.ClienteEmail))
                {
                    Console.WriteLine("❌ O Email está vazio → DLQ");
                    await channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: false);
                    return;
                }


                if(simularErroTemporario)
                    throw new Exception("Erro temporário simulado para teste de retry!");

                //if (Random.Shared.Next(0, 10) == 5)
                //    throw new Exception("Banco temporariamente indisponível");

                await Task.Delay(2000);
                Console.WriteLine($"✅ Pedido Processado com sucesso: {pedido.Id}");

                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            catch (JsonException ex) // Erro específico para JSON corrompido ou malformado
            {
                Console.WriteLine($"❌ JSON corrompido: {ex.Message} → DLQ");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
            catch (Exception ex) // Erro genérico para outros tipos de falhas (temporárias ou não)
            {
                Console.WriteLine($"⚠️ Erro temporário: {ex.Message} → Retry");

                // Vai tentar reenviar para a fila de retry, incrementando o contador de tentativas. Se o limite for excedido, envia para a DLQ.
                if (retryCount < Retries.MaxRetryAttempts - 1)
                {
                    var newRetryCount = retryCount + 1;

                    var headers = new Dictionary<string, object?>
                    {
                        { "x-retry-count", newRetryCount }
                    };

                    var retryProperties = new BasicProperties
                    {
                        DeliveryMode = DeliveryModes.Persistent,
                        ContentType = "application/json",
                        ContentEncoding = "utf-8",
                        Headers = headers
                    };

                    await channel.BasicPublishAsync(exchange: Exchanges.Retry, routingKey: RoutingKeys.PedidoRetry, mandatory: false, basicProperties: retryProperties, body: eventArgs.Body.ToArray());

                    Console.WriteLine($"Enviado para retry (Tentativa {newRetryCount + 1} em {Retries.RetryDelayMs}ms)");
                    Console.WriteLine();

                    await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                }
                else
                {
                    Console.WriteLine($"❌ Limite excedido → DLQ");
                    await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                }

                //await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
            }
        }
    }
}
