using Microsoft.EntityFrameworkCore;
using PayFlow.PaymentService.Data;
using PayFlow.PaymentService.Repositories;
using PayFlow.Shared;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<PaymentDbContext>(options =>
 options.UseSqlServer("Server=localhost,1433;Database=PayFlowDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"));

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("localhost:6379"));

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
