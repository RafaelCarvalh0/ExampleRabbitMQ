namespace RabbitMQ.Models.Models.Pedido
{
    public class PedidoItemRequest
    {
        public required string NomeProduto { get; set; }
        public required int Quantidade { get; set; }
        public required decimal PrecoUnitario { get; set; }
    }
}
