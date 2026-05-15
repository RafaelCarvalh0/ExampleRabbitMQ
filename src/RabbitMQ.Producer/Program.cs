using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Models.Models.Pedido;
using RabbitMQ.Producer.Handlers;
using RabbitMQ.Shared.Infrastructure;
using RabbitMQ.Shared.Messaging;

var builder = Host.CreateApplicationBuilder(args);

var rabbitSettings = builder
    .Configuration
    .GetSection("RabbitMqSettings")
    .Get<RabbitMqSettings>()!;

await using var connection = await RabbitMqConnectionFactory.CreateConnectionAsync(rabbitSettings);

await using var channel = await connection.CreateChannelAsync();

await RabbitMqQueueSetup.ConfigureAsync(channel);

var publisher = new PedidoPublisher(channel);

while (true)
{
    Console.WriteLine();
    Console.WriteLine("Quantos pedidos você quer enviar? (0 para sair)");

    if (!int.TryParse(Console.ReadLine(), out int quantidade))
    {
        Console.WriteLine("❌ Valor inválido.");
        continue;
    }

    if (quantidade == 0)
    {
        break;
    }

    Console.WriteLine();
    Console.WriteLine("Deseja enviar pedidos com erro? [S]Sim [N]Não");

    bool criarPedidoComErro =
        Console.ReadLine()?.Trim().ToLower() == "s";

    Console.WriteLine();

    for (int i = 0; i < quantidade; i++)
    {
        PedidoRequest pedido = !criarPedidoComErro ? PedidoFakeFactory.Criar(i) : PedidoFakeFactory.CriarComErro(i);

        await publisher.PublicarAsync(pedido);

        Console.WriteLine($"✅ Pedido {pedido.Id} enviado!");
    }

    Console.WriteLine();
    Console.WriteLine($"✅ {quantidade} pedidos enviados.");
}

Console.WriteLine();
Console.WriteLine("👋 Aplicação encerrada.");