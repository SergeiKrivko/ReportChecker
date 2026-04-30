using System.Globalization;

namespace YooMoney;

internal class QuickPay
{
    public required string Receiver { get; init; }
    public string QuickPayForm { get; init; } = "button";
    public PaymentSource Source { get; init; }
    public required decimal Sum { get; init; }
    public required string Label { get; init; }
    public string? Comment { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? FathersName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? City { get; init; }
    public string? Street { get; init; }
    public string? Building { get; init; }
    public string? Suite { get; init; }
    public string? Flat { get; init; }
    public string? Zip { get; init; }
    public string? Sender { get; init; }
    public string? SuccessRedirectUrl { get; init; }
}

internal enum PaymentSource
{
    Wallet,
    BankCard,
}