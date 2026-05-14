using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace RabbitMQ.Models.Models;

public class Pedido
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public required Guid Id { get; set; }
    public required string ClienteEmail { get; set; }
    public required decimal ValorTotal { get; set; }

    [BsonSerializer(typeof(DateTimeOffsetSerializer))]
    public required DateTimeOffset DataCriacao { get; set; }
    public required List<Item> Itens { get; set; }
}