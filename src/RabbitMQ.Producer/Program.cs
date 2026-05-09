using RabbitMQ.Producer.Handlers;
using RabbitMQ.Shared.Infrastructure;
using RabbitMQ.Shared.Messaging;

var settings = new RabbitMqSettings();
await using var connection = await RabbitMqConnectionFactory.CreateAsync(settings);
await using var channel = await connection.CreateChannelAsync();

await RabbitMqQueueSetup.ConfigureAsync(channel);

var publisher = new PedidoPublisher(channel);

Console.WriteLine("Quantos pedidos você quer enviar?");
if (int.TryParse(Console.ReadLine(), out int quantidade))
{
    for (int i = 0; i < quantidade; i++)
    {
        var pedido = PedidoFakeFactory.CriarComErro(i);
        await publisher.PublicarAsync(pedido);
        Console.WriteLine("ENTER para o próximo...");
        Console.ReadLine();
    }
}