using MongoDB.Driver;
using RabbitMQ.Shared.Messaging;
using RabbitMQ.Consumer.Models;

namespace RabbitMQ.Consumer.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly IMongoCollection<PedidoProcessado> _collection;
    private readonly ILogger<PedidoRepository> _logger;

    public PedidoRepository(MongoDbSettings settings, ILogger<PedidoRepository> logger)
    {
        _logger = logger;

        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        _collection = database.GetCollection<PedidoProcessado>(settings.CollectionName);

        _logger.LogInformation("MongoDB conectado — database: {Db}", settings.DatabaseName);
    }

    public async Task SaveAsync(PedidoProcessado pedido)
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
            _logger.LogWarning("Pedido {Id} já existe no banco — duplicata ignorada",pedido.PedidoId);
        }
    }

    public async Task<bool> ExistsAsync(Guid pedidoId)
    {
        var filter = Builders<PedidoProcessado>.Filter.Eq(p => p.PedidoId, pedidoId);
        return await _collection.Find(filter).AnyAsync();
    }
}