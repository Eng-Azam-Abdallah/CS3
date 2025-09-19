using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RofaqaaApi.Models;

public class Expense
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public ExpenseCategory Category { get; set; }

    public int GroupId { get; set; }

    // العلاقات
    [ForeignKey("GroupId")]
    public Group Group { get; set; } = null!;

    public ICollection<ExpenseShare> Shares { get; set; } = new List<ExpenseShare>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public enum ExpenseCategory
{
    Food = 0,
    Utilities = 1,
    Entertainment = 2,
    Transportation = 3,
    Shopping = 4,
    Other = 5
}