namespace RofaqaaApi.DTOs;

public class MemberBalanceDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public decimal TotalPaid { get; set; }     // إجمالي ما دفعه
    public decimal TotalShare { get; set; }    // إجمالي ما استفاد منه
    public decimal NetBalance { get; set; }    // الرصيد الصافي (إجمالي ما دفعه - إجمالي ما استفاد منه)
    public List<ExpenseBreakdownDto> Breakdown { get; set; } = new();
}

public class ExpenseBreakdownDto
{
    public int ExpenseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }   // المبلغ الكلي للمصروف
    public decimal PaidAmount { get; set; }    // المبلغ الذي دفعه العضو
    public decimal ShareAmount { get; set; }   // المبلغ الذي عليه دفعه (حصته)
    public string ShareType { get; set; } = string.Empty;
    public decimal? SharePercentage { get; set; }
}

public class GroupBalanceSheetDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public decimal TotalExpenses { get; set; }
    public List<MemberBalanceDto> MemberBalances { get; set; } = new();
    public List<DebtSettlementDto> Settlements { get; set; } = new();
}

public class DebtSettlementDto
{
    public int FromUserId { get; set; }
    public string FromUsername { get; set; } = string.Empty;
    public int ToUserId { get; set; }
    public string ToUsername { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class DetailedMemberBalanceDto
{
    public MemberBalanceDto Balance { get; set; } = new();
    public List<DetailedTransactionDto> Transactions { get; set; } = new();
}

public class DetailedTransactionDto
{
    public int ExpenseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty; // "Payment" or "Share"
    public string ShareType { get; set; } = string.Empty;
    public decimal? Percentage { get; set; }
    public List<string> OtherParticipants { get; set; } = new();
}