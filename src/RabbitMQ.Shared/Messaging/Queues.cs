namespace RabbitMQ.Shared.Messaging
{
    public static class Queues
    {
        public const string Principal = "pedido.criados";
        public const string Dlq = "pedido.dlq";
        public const string Retry = "pedido.retry";
        public const string Processado = "pedido.processado";
    }
}
