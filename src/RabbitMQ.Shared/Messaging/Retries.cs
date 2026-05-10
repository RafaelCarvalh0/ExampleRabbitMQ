namespace RabbitMQ.Shared.Messaging
{
    public static class Retries
    {
        public const int MaxRetryAttempts = 3;
        public const long RetryDelayMs = 5000;
    }
}
