using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using RabbitMQ.Application.Services;
using RabbitMQ.Application.Services.Handlers;
using RabbitMQ.Application.Services.Workers;
using RabbitMQ.Infrasctructure.Repositories;
using RabbitMQ.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Serializer DateTimeOffset → string ISO
BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));

var rabbitSettings = builder.Configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>()!;
var mongoSettings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>()!;

builder.Services.AddSingleton(rabbitSettings);
builder.Services.AddSingleton(mongoSettings);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSignalR();

builder.Services.AddSingleton<PedidoApplicationHandler>();

// Repositório
builder.Services.AddSingleton<IPedidoRepository, PedidoRepository>();
builder.Services.AddHostedService<PedidoConsumerWorker>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}")
//    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Pedido}/{action=Index}/{id?}");

// Registra o hub — URL que o JavaScript vai conectar
app.MapHub<PedidoHub>("/pedidoHub");

app.Run();
