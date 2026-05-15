using RabbitMQ.Models.Models.Pedido;

namespace RabbitMQ.Infrasctructure.Repositories
{
    public interface IPedidoRepository
    {
        Task SaveAsync(PedidoProcessadoEntity pedido);
        Task<bool> ExistsAsync(Guid id);
        Task<List<PedidoProcessadoEntity>> GetAllAsync();
        Task<bool> DeleteAsync(Guid pedidoId);
    }
}
