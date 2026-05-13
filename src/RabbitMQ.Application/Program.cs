using RabbitMQ.Application.Services;
using RabbitMQ.Application.Services.Handlers;
using RabbitMQ.Application.Services.Workers;
using RabbitMQ.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var rabbitSettings = builder.Configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>();
builder.Services.AddSingleton<PedidoHandler>();

builder.Services.AddSingleton(rabbitSettings);
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
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
