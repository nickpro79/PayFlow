using Confluent.Kafka;
using PayFlow.Shared.Messaging;
using System.Text.Json;

namespace PayFlow.PaymentService.Messaging;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IConfiguration configuration, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _logger.LogInformation("Kafka producer configured with bootstrap servers: {BootstrapServers}", bootstrapServers);

        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, T eventData)
    {
        try
        {
            var json = JsonSerializer.Serialize(eventData);
            var result = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = json
            });
            _logger.LogInformation("Published event to {Topic} at offset {Offset}", topic, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish event to Kafka topic {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
    }
}