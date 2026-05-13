using RabbitMQ.Model.Models;
using RabbitMQ.Consumer.Models;

namespace RabbitMQ.Consumer.Repositories
{
    public interface IPedidoRepository
    {
        Task SaveAsync(PedidoProcessado pedido);
        Task<bool> ExistsAsync(Guid id);
    }
}
