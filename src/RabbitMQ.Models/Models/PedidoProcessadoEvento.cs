namespace RabbitMQ.Models
{
    public class PedidoProcessadoEvento
    {
        public Guid PedidoId { get; set; }
        public string ClienteEmail { get; set; }
        public decimal ValorTotal { get; set; }
        public string Status { get; set; }
        public string? Motivo { get; set; }
        public DateTimeOffset ProcessadoEm { get; set; }
    }
}
