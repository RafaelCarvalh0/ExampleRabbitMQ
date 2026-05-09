using RabbitMQ.Model.Models;

namespace RabbitMQ.Producer.Handlers
{
    public static class PedidoFakeFactory
    {
        public static Pedido Criar(int index)
        {
            return new Pedido
            {
                Id = Guid.NewGuid(),
                ClienteEmail = $"cliente{index}@email.com",
                ValorTotal = new Random().Next(100, 5000),
                DataCriacao = DateTimeOffset.UtcNow,
                Itens = new List<Item>
            {
                new Item
                {
                    NomeProduto = $"Produto {index}",
                    Quantidade = new Random().Next(1, 5),
                    PrecoUnitario = new Random().Next(20, 1000)
                }
            }
            };
        }

        public static Pedido CriarComErro(int index)
        {
            return new Pedido
            {
                Id = Guid.NewGuid(),
                ClienteEmail = $"cliente{index}@email.com",
                ValorTotal = -100, // Vai cair na DLQ
                DataCriacao = DateTimeOffset.UtcNow,
                Itens = new List<Item>
            {
                new Item
                {
                    NomeProduto = $"Produto {index}",
                    Quantidade = new Random().Next(1, 5),
                    PrecoUnitario = new Random().Next(20, 1000)
                }
            }
            };
        }
    }
}
