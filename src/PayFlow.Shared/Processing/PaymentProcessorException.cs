namespace PayFlow.Shared.Processing;

public class PaymentProcessorException : Exception
{
    public PaymentProcessorException(string message) : base(message) { }
}