namespace RabbitMQ.Models.Models.Pedido;

public class PedidoRequest
{
    public required Guid Id { get; set; }
    public required string ClienteEmail { get; set; }
    public required decimal ValorTotal { get; set; }
    public required DateTimeOffset DataCriacao { get; set; }
    public required List<PedidoItemRequest> Itens { get; set; }
}