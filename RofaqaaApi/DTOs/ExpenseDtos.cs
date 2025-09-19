using System.ComponentModel.DataAnnotations;

namespace RofaqaaApi.DTOs;

public class CreateExpenseDto
{
    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "يجب أن يكون المبلغ أكبر من صفر")]
    public decimal Amount { get; set; }

    [Required]
    public string Category { get; set; } = "Other";

    [Required]
    public List<ExpenseShareDto> Shares { get; set; } = new();

    [Required]
    public List<ExpensePaymentDto> Payments { get; set; } = new();
}

public class ExpenseShareDto
{
    [Required]
    public int GroupMemberId { get; set; }

    [Required]
    public string Type { get; set; } = "Equal"; // Equal, Fixed, Percentage

    public decimal? Amount { get; set; }
    public decimal? Percentage { get; set; }
}

public class ExpensePaymentDto
{
    [Required]
    public int GroupMemberId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "يجب أن يكون المبلغ أكبر من صفر")]
    public decimal Amount { get; set; }
}

public class ExpenseDetailsDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ShareDetailDto> Shares { get; set; } = new();
    public List<PaymentDetailDto> Payments { get; set; } = new();
}

public class ShareDetailDto
{
    public int Id { get; set; }
    public int GroupMemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? Percentage { get; set; }
}

public class PaymentDetailDto
{
    public int Id { get; set; }
    public int GroupMemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
}

public class ExpenseSummaryDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int PayersCount { get; set; }
    public int SharesCount { get; set; }
}