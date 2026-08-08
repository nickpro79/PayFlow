using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Shared;
using PayFlow.Shared.Events;
using PayFlow.Shared.Messaging;
using PayFlow.Shared.Processing;
using Polly.CircuitBreaker;
using StackExchange.Redis;
using System.Text.Json;

namespace PayFlow.PaymentService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly IConnectionMultiplexer _redis;
        private readonly IEventPublisher _eventPublisher;
        private readonly IPaymentProcessor _paymentProcessor;

        public PaymentsController(IPaymentRepository paymentRepository, IConnectionMultiplexer redis, IEventPublisher eventPublisher, IPaymentProcessor paymentProcessor)
        {
            _paymentRepository = paymentRepository;
            _redis = redis;
            _eventPublisher = eventPublisher;
            _paymentProcessor = paymentProcessor;
        }

        public record CreatePaymentRequest(decimal Amount, string Currency);
        [HttpPost]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request, [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return BadRequest("Idempotency-Key header is required.");

            var cache = _redis.GetDatabase();
            var cachedResult = await cache.StringGetAsync(idempotencyKey);

            if (cachedResult.HasValue)
            {
                var cached = JsonSerializer.Deserialize<Payment>((string)cachedResult!);
                return Ok(cached);
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                IdempotencyKey = idempotencyKey,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = "PENDING"
            };

            await _paymentRepository.AddAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            // Simulate calling an external payment processor
            try
            {
                await _paymentProcessor.ProcessAsync(payment.Amount, payment.Currency);
                payment.Status = "SUCCESS";
            }
            catch (BrokenCircuitException)
            {
                payment.Status = "FAILED";
                await _paymentRepository.UpdateAsync(payment);
                await _paymentRepository.SaveChangesAsync();
                return StatusCode(503, new { error = "Payment processor is temporarily unavailable. Please try again shortly." });
            }
            catch (PaymentProcessorException)
            {
                payment.Status = "FAILED";
                await _paymentRepository.UpdateAsync(payment);
                await _paymentRepository.SaveChangesAsync();
                return StatusCode(502, new { error = "Payment could not be processed after multiple attempts." });
            }

            await _paymentRepository.UpdateAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            await cache.StringSetAsync(idempotencyKey, JsonSerializer.Serialize(payment), TimeSpan.FromHours(24));

            await _eventPublisher.PublishAsync("payment-events", new PaymentSucceededEvent
            {
                PaymentId = payment.Id,
                Amount = payment.Amount,
                Currency = payment.Currency
            });
            return Ok(payment);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(Guid id)
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            return payment is null ? NotFound() : Ok(payment);
        }

    }

}
