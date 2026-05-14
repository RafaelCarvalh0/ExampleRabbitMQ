using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RabbitMQ.Models.Models
{
    public class PedidoProcessado
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid PedidoId { get; set; }
        public string ClienteEmail { get; set; }
        public decimal ValorTotal { get; set; }
        public DateTimeOffset DataCriacao { get; set; }
        public List<Item> Itens { get; set; } = new();

        // Campos de controle do Worker
        public DateTimeOffset ProcessadoEm { get; set; }
        public string Status { get; set; }
        public string? Motivo { get; set; }
        public int? Tentativas { get; set; }

        // Factory — constrói a partir do Pedido recebido
        public static PedidoProcessado FromPedido(Pedido pedido, string status, string? motivo, int? tentativas) => new()
        {
            PedidoId = pedido.Id,
            ClienteEmail = pedido.ClienteEmail,
            ValorTotal = pedido.ValorTotal,
            DataCriacao = pedido.DataCriacao,
            Itens = pedido.Itens,
            ProcessadoEm = DateTimeOffset.UtcNow,
            Status = status,
            Motivo = motivo,
            Tentativas = tentativas
        };
    }
}
