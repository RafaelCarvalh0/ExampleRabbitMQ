using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Model.Models;
using System.Text.Json;

namespace RabbitMQ.Consumer.Handlers
{
    public class PedidoHandler(IChannel channel)
    {
        public async Task HandleAsync(object sender, BasicDeliverEventArgs eventArgs)
        {
            try
            {
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

                if (Random.Shared.Next(0, 10) == 5)
                    throw new Exception("Banco temporariamente indisponível");

                await Task.Delay(2000);
                Console.WriteLine($"✅ Pedido Processado com sucesso: {pedido.Id}");

                await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"❌ JSON corrompido: {ex.Message} → DLQ");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erro temporário: {ex.Message} → Retry");
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: true);
            }
        }
    }
}
