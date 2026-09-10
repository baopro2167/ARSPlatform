using System;
using System.Collections.Generic;

namespace ARSPlatform.SERVICE.DTOs.Response;

/// <summary>
/// AnnualFee response
/// </summary>
public class AnnualFeeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string UserRole { get; set; } = null!;
    public decimal Price { get; set; }
    public string BillingCycle { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Subscription hiện tại của user
/// </summary>
public class UserSubscriptionResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserRole { get; set; } = null!;
    public DateTime? ExpiresAt { get; set; }
    public int? LatestTransactionId { get; set; }
    public DateTime? DaysRemaining { get; set; } // số ngày còn lại
    public bool IsExpired { get; set; }
    public AnnualFeeResponse? AnnualFee { get; set; }
}

/// <summary>
/// Purchase history response (từ Transactions)
/// </summary>
public class AnnualFeePurchaseResponse
{
    public int TransactionId { get; set; }
    public int? UserId { get; set; }
    public int? AnnualFeeId { get; set; }
    public decimal? Amount { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
    public string? PaymentDescription { get; set; }
    public string? PaymentMethod { get; set; }
    public string? PaymentOrderId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public AnnualFeeResponse? AnnualFee { get; set; }
}

/// <summary>
/// Purchase result trả về cho FE
/// </summary>
public class AnnualFeePurchaseResultResponse
{
    public string CheckoutUrl { get; set; } = null!;
    public string OrderCode { get; set; } = null!;
    public AnnualFeePurchaseResponse Purchase { get; set; } = null!;
}

/// <summary>
/// My subscription result (annualFee + daysRemaining)
/// </summary>
public class MySubscriptionResponse
{
    public AnnualFeePurchaseResponse? Purchase { get; set; }
    public AnnualFeeResponse? AnnualFee { get; set; }
    public int? DaysRemaining { get; set; }
    public bool IsExpired { get; set; }
}
