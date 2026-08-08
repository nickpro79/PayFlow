using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using PayFlow.Shared.Processing;

namespace PayFlow.PaymentService.Processing;

public class ResilientPaymentProcessor : IPaymentProcessor
{
    private readonly MockPaymentProcessor _innerProcessor;
    private readonly ResiliencePipeline<bool> _pipeline;
    private readonly ILogger<ResilientPaymentProcessor> _logger;

    public ResilientPaymentProcessor(MockPaymentProcessor innerProcessor, ILogger<ResilientPaymentProcessor> logger)
    {
        _innerProcessor = innerProcessor;
        _logger = logger;

        _pipeline = new ResiliencePipelineBuilder<bool>()
            // Circuit breaker is now OUTER — it only sees one outcome per logical request (after retries are exhausted or succeeded)
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<bool>
            {
                ShouldHandle = new PredicateBuilder<bool>().Handle<PaymentProcessorException>(),
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 4,
                BreakDuration = TimeSpan.FromSeconds(15),
                OnOpened = args =>
                {
                    _logger.LogError("Circuit breaker OPENED — payment processor calls suspended for {Seconds}s", args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation("Circuit breaker CLOSED — resuming normal calls");
                    return ValueTask.CompletedTask;
                }
            })
            // Retry is now INNER — retries happen first, and only the final result bubbles up to the circuit breaker
            .AddRetry(new RetryStrategyOptions<bool>
            {
                ShouldHandle = new PredicateBuilder<bool>().Handle<PaymentProcessorException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(300),
                BackoffType = DelayBackoffType.Exponential,
                OnRetry = args =>
                {
                    _logger.LogWarning("Payment processor call failed, retrying (attempt {Attempt})...", args.AttemptNumber + 1);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    public async Task<bool> ProcessAsync(decimal amount, string currency)
    {
        return await _pipeline.ExecuteAsync(async _ => await _innerProcessor.ProcessAsync(amount, currency));
    }
}