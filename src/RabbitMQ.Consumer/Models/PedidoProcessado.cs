using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RabbitMQ.Model.Models;

namespace RabbitMQ.Consumer.Models;

public class PedidoProcessado
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid PedidoId { get; set; }
    public string ClienteEmail { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public DateTimeOffset DataCriacao { get; set; }
    public List<Item> Itens { get; set; } = new();

    // Campos de controle do Worker
    public DateTimeOffset ProcessadoEm { get; set; }
    public string Status { get; set; } = "Processado";
    public int Tentativas { get; set; }

    // Factory — constrói a partir do Pedido recebido
    public static PedidoProcessado FromPedido(Pedido pedido, int tentativas) => new()
    {
        PedidoId = pedido.Id,
        ClienteEmail = pedido.ClienteEmail,
        ValorTotal = pedido.ValorTotal,
        DataCriacao = pedido.DataCriacao,
        Itens = pedido.Itens,
        ProcessadoEm = DateTimeOffset.UtcNow,
        Status = "Processado",
        Tentativas = tentativas
    };
}