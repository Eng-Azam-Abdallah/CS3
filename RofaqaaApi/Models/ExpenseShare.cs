using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RofaqaaApi.Models;

public class ExpenseShare
{
    [Key]
    public int Id { get; set; }

    public int ExpenseId { get; set; }
    public int GroupMemberId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public ShareType Type { get; set; }

    public decimal? Percentage { get; set; }

    // العلاقات
    [ForeignKey("ExpenseId")]
    public Expense Expense { get; set; } = null!;

    [ForeignKey("GroupMemberId")]
    public GroupMember GroupMember { get; set; } = null!;
}

public enum ShareType
{
    Equal = 0,           // تقسيم متساوي بين الأعضاء
    Fixed = 1,          // مبلغ محدد
    Percentage = 2      // نسبة مئوية من المبلغ الكلي
}