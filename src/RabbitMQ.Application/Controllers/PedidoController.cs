using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Application.Services;
using RabbitMQ.Infrasctructure.Repositories;

namespace RabbitMQ.Application.Controllers
{
    public class PedidoController : Controller
    {
        private readonly IPedidoRepository _repository;
        private readonly IHubContext<PedidoHub> _hub;

        public PedidoController(IPedidoRepository repository, IHubContext<PedidoHub> hub)
        {
            _repository = repository;
            _hub = hub;
        }

        public IActionResult Index()
        {
            return View();
        }
        
        public IActionResult Historico()
        {
            return View();
        }

        // GET /api/pedidos
        [HttpGet("/api/pedidos")]
        public async Task<IActionResult> GetPedidos()
        {
            var pedidos = await _repository.GetAllAsync();
            return Ok(pedidos);
        }

        // DELETE /api/pedidos/{id}
        [HttpDelete("/api/pedidos/{id}")]
        public async Task<IActionResult> DeletePedido(Guid id)
        {
            var deleted = await _repository.DeleteAsync(id);

            if (!deleted)
                return NotFound(new { message = "Pedido não encontrado" });

            // Notifica todos os browsers conectados
            await _hub.Clients.All.SendAsync("PedidoRemovido", id);

            return Ok(new { message = "Pedido removido" });
        }
    }
}
