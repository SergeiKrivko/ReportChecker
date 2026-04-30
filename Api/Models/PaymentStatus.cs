namespace ReportChecker.Models;

public enum PaymentStatus
{
    Wait = 0,
    Succeeded = 1,
    Failed = 2,
    RequireRefund = 3,
}