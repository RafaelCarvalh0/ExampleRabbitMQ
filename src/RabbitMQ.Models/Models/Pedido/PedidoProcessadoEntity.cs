using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;
using RabbitMQ.Models.Models.Pedido.Enums;
using System.Text.Json.Serialization;

namespace RabbitMQ.Models.Models.Pedido
{
    public class PedidoProcessadoEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid PedidoId { get; set; }
        public string ClienteEmail { get; set; }
        public decimal ValorTotal { get; set; }

        [BsonSerializer(typeof(DateTimeOffsetSerializer))]
        public DateTimeOffset DataCriacao { get; set; }
        public List<PedidoItemRequest> Itens { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StatusPedido Status { get; set; }
        public DateTimeOffset ProcessadoEm { get; set; }
        public string? Motivo { get; set; }
        public int? Tentativas { get; set; }

        public static PedidoProcessadoEntity FromPedido(PedidoRequest pedido, StatusPedido status, string? motivo = null, int? tentativas = null) => new()
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
