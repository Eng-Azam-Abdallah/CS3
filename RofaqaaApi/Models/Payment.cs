using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RofaqaaApi.Models;

public class Payment
{
    [Key]
    public int Id { get; set; }

    public int ExpenseId { get; set; }
    public int GroupMemberId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    // العلاقات
    [ForeignKey("ExpenseId")]
    public Expense Expense { get; set; } = null!;

    [ForeignKey("GroupMemberId")]
    public GroupMember GroupMember { get; set; } = null!;
}