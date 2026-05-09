namespace RabbitMQ.Model.Models
{
    public class Pedido
    {
        public required Guid Id { get; set; }
        public required string ClienteEmail { get; set; }
        public required decimal ValorTotal { get; set; }
        public required DateTimeOffset DataCriacao { get; set; }
        public required List<Item> Itens { get; set; }
    }
}
    