
using RabbitMQ.Models.Models;

namespace RabbitMQ.Infrasctructure.Repositories
{
    public interface IPedidoRepository
    {
        Task SaveAsync(PedidoProcessado pedido);
        Task<bool> ExistsAsync(Guid id);
        Task<List<PedidoProcessado>> GetAllAsync();
        Task<bool> DeleteAsync(Guid pedidoId);
    }
}
