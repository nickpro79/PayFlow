using Confluent.Kafka;
using PayFlow.Shared.Events;
using System.Text.Json;

namespace PayFlow.NotificationService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private IConsumer<string, string>? _consumer;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = _configuration["Kafka:GroupId"] ?? "notification-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false // we'll commit manually after successfully processing each message
        };

        var topic = _configuration["Kafka:Topic"] ?? "payment-events";

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer = consumer;
        consumer.Subscribe(topic);

        _logger.LogInformation("Notification Service started. Listening on topic: {Topic}", topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);

                    if (result?.Message?.Value is not null)
                    {
                        await ProcessMessageAsync(result.Message.Value);
                        consumer.Commit(result); // manually commit offset only after successful processing
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        finally
        {
            consumer.Close();
        }
    }

    private Task ProcessMessageAsync(string messageValue)
    {
        try
        {
            var paymentEvent = JsonSerializer.Deserialize<PaymentSucceededEvent>(messageValue);

            if (paymentEvent is not null)
            {
                // Simulated notification — in a real system, this would call an email/SMS provider (e.g. SendGrid, Twilio)
                _logger.LogInformation(
                    "📧 Sending confirmation notification for Payment {PaymentId} — {Amount} {Currency}",
                    paymentEvent.PaymentId, paymentEvent.Amount, paymentEvent.Currency);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize payment event: {Message}", messageValue);
        }

        return Task.CompletedTask;
    }
}