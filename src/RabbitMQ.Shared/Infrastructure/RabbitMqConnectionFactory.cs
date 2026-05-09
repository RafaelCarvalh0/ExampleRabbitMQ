using RabbitMQ.Client;
using RabbitMQ.Shared.Messaging;

namespace RabbitMQ.Shared.Infrastructure
{
    public static class RabbitMqConnectionFactory
    {
        public static async Task<IConnection> CreateAsync(RabbitMqSettings settings)
        {
            var factory = new ConnectionFactory
            {
                HostName = settings.HostName,
                Port = settings.Port,
                UserName = settings.UserName,
                Password = settings.Password,
                VirtualHost = settings.VirtualHost,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            return await factory.CreateConnectionAsync();
        }
    }
}
