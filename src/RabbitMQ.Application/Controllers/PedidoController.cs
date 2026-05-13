using Microsoft.AspNetCore.Mvc;

namespace RabbitMQ.Application.Controllers
{
    public class PedidoController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
