using RabbitMQ.Model.Models;
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
    Console.WriteLine();
    Console.WriteLine("Deseja enviar um pedido fake com erro? [1]Sim [2]Não");
    bool criarPedidoComErro = Console.ReadLine()?.Trim().ToLower() == "1";

    for (int i = 0; i < quantidade; i++)
    {
        Pedido pedido = !criarPedidoComErro ? PedidoFakeFactory.Criar(i) : PedidoFakeFactory.CriarComErro(i);
        await publisher.PublicarAsync(pedido);
        Console.WriteLine("ENTER para o próximo...");
        Console.ReadLine();
    }
}