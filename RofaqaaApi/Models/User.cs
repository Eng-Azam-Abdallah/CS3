using System.ComponentModel.DataAnnotations;

namespace RofaqaaApi.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // التحقق من الحساب
    public bool IsEmailVerified { get; set; }
    public string? EmailVerificationToken { get; set; }
    
    // العلاقات
    public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
}