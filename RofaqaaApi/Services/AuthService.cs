using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RofaqaaApi.Data;
using RofaqaaApi.DTOs;
using RofaqaaApi.Models;

namespace RofaqaaApi.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto model);
    Task<AuthResponseDto> LoginAsync(LoginDto model);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto model)
    {
        if (model.Password != model.ConfirmPassword)
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "كلمة المرور وتأكيدها غير متطابقين"
            };
        }

        // التحقق من عدم وجود البريد الإلكتروني مسبقاً
        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "البريد الإلكتروني مستخدم مسبقاً"
            };
        }

        // التحقق من عدم وجود اسم المستخدم مسبقاً
        if (await _context.Users.AnyAsync(u => u.Username == model.Username))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "اسم المستخدم مستخدم مسبقاً"
            };
        }

        // إنشاء مستخدم جديد
        var user = new User
        {
            Username = model.Username,
            Email = model.Email,
            PasswordHash = HashPassword(model.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // إنشاء وإرجاع التوكن
        var token = GenerateJwtToken(user);
        return new AuthResponseDto
        {
            Success = true,
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            }
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto model)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !VerifyPassword(model.Password, user.PasswordHash))
        {
            return new AuthResponseDto
            {
                Success = false,
                Message = "البريد الإلكتروني أو كلمة المرور غير صحيحة"
            };
        }

        // تحديث وقت آخر تسجيل دخول
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // إنشاء وإرجاع التوكن
        var token = GenerateJwtToken(user);
        return new AuthResponseDto
        {
            Success = true,
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            }
        };
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? 
                throw new InvalidOperationException("JWT Key is not configured")));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(_configuration["Jwt:DurationInMinutes"] ?? "60")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}