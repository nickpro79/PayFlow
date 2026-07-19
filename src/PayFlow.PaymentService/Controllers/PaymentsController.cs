using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PayFlow.Shared;
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

        public PaymentsController(IPaymentRepository paymentRepository, IConnectionMultiplexer redis)
        {
            _paymentRepository = paymentRepository;
            _redis = redis;
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
            await Task.Delay(200);
            payment.Status = "SUCCESS";
            await _paymentRepository.UpdateAsync(payment);
            await _paymentRepository.SaveChangesAsync();

            await cache.StringSetAsync(idempotencyKey, JsonSerializer.Serialize(payment), TimeSpan.FromHours(24));

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
