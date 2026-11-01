// Models/DTOs/ApiPaymentDtos.cs

namespace ServiceHub_IT.DTOs.Api;

public sealed class SetPaymentMethodRequest
{
    public string? PaymentMethod { get; set; }
}