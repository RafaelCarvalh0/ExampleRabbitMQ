using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using RabbitMQ.Consumer;
using RabbitMQ.Consumer.Repositories;
using RabbitMQ.Shared.Messaging;

var builder = Host.CreateApplicationBuilder(args);

// Lê configurações do appsettings.json
var mongoSettings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>()!;

var rabbitSettings = builder.Configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>() ?? new RabbitMqSettings();

// Registra serializer global para DateTimeOffset
BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));

builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton(rabbitSettings);

// Repositório e Handler
builder.Services.AddSingleton<IPedidoRepository, PedidoRepository>();
builder.Services.AddSingleton<PedidoHandler>();
builder.Services.AddHostedService<Worker>();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Worker.Consumer";
});

var host = builder.Build();
host.Run();