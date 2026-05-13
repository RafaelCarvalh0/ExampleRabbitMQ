using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Model.Models;
using RabbitMQ.Producer.Handlers;
using RabbitMQ.Shared.Infrastructure;
using RabbitMQ.Shared.Messaging;

var builder = Host.CreateApplicationBuilder(args);
var rabbitSettings = builder.Configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>()!;

await using var connection = await RabbitMqConnectionFactory.CreateAsync(rabbitSettings);
await using var channel = await connection.CreateChannelAsync();

await RabbitMqQueueSetup.ConfigureAsync(channel);

var publisher = new PedidoPublisher(channel);

Console.WriteLine("Quantos pedidos você quer enviar?");
if (int.TryParse(Console.ReadLine(), out int quantidade))
{
    Console.WriteLine();
    Console.WriteLine("Deseja enviar um pedido fake com erro? [S]Sim [N]Não");
    bool criarPedidoComErro = Console.ReadLine()?.Trim().ToLower() == "s";

    for (int i = 0; i < quantidade; i++)
    {
        Pedido pedido = !criarPedidoComErro ? PedidoFakeFactory.Criar(i) : PedidoFakeFactory.CriarComErro(i);
        await publisher.PublicarAsync(pedido);
        Console.WriteLine($"✅ Pedido {pedido.Id} enviado!");
    }

    Console.WriteLine($"\n{quantidade} pedidos enviados.");
}