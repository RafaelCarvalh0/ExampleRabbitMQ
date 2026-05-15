using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Models.Models.Pedido;
using RabbitMQ.Shared.Messaging;

namespace RabbitMQ.Infrasctructure.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly IMongoCollection<PedidoProcessadoEntity> _collection;
        private readonly ILogger<PedidoRepository> _logger;

        public PedidoRepository(MongoDbSettings settings, ILogger<PedidoRepository> logger)
        {
            _logger = logger;

            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);
            _collection = database.GetCollection<PedidoProcessadoEntity>(settings.CollectionName);

            _logger.LogInformation("MongoDB conectado — database: {Db}", settings.DatabaseName);
        }

        public async Task SaveAsync(PedidoProcessadoEntity pedido)
        {
            try
            {
                await _collection.InsertOneAsync(pedido);
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Mensagem duplicada — ignora silenciosamente
                // O ExistsAsync deveria ter pego antes, mas RabbitMQ
                // pode entregar a mesma mensagem em paralelo
                _logger.LogWarning("Pedido {Id} já existe no banco — duplicata ignorada", pedido.PedidoId);
            }
        }

        public async Task<bool> ExistsAsync(Guid pedidoId)
        {
            var filter = Builders<PedidoProcessadoEntity>.Filter.Eq(p => p.PedidoId, pedidoId);
            return await _collection.Find(filter).AnyAsync();
        }

        public async Task<List<PedidoProcessadoEntity>> GetAllAsync()
        {
            return await _collection
            .Find(_ => true)
            .SortByDescending(p => p.ProcessadoEm)
            .ToListAsync();
        }

        public async Task<bool> DeleteAsync(Guid pedidoId)
        {
            var filter = Builders<PedidoProcessadoEntity>.Filter
            .Eq(p => p.PedidoId, pedidoId);

            var result = await _collection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
    }
}
