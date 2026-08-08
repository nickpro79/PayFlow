using PayFlow.Shared.Processing;

namespace PayFlow.PaymentService.Processing;

public class MockPaymentProcessor : IPaymentProcessor
{
    private static readonly Random _random = new();

    public async Task<bool> ProcessAsync(decimal amount, string currency)
    {
        // Simulate network latency
        await Task.Delay(200);

        // Simulate a ~30% failure rate, like a flaky external payment gateway
        if (_random.Next(1, 101) <= 30)
        {
            throw new PaymentProcessorException("Simulated payment processor failure (timeout/network issue).");
        }

        return true;
    }
}