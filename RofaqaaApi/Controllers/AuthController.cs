using Microsoft.AspNetCore.Mvc;
using RofaqaaApi.DTOs;
using RofaqaaApi.Services;
using System.ComponentModel.DataAnnotations;

namespace RofaqaaApi.Controllers;

/// <summary>
/// يتحكم في عمليات المصادقة وإدارة المستخدمين
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// تسجيل مستخدم جديد في النظام
    /// </summary>
    /// <param name="model">بيانات التسجيل المطلوبة</param>
    /// <remarks>
    /// مثال طلب التسجيل:
    /// 
    ///     POST /api/auth/register
    ///     {
    ///         "username": "عزام",
    ///         "email": "azam@example.com",
    ///         "password": "كلمة_المرور_123"
    ///     }
    /// </remarks>
    /// <response code="200">تم التسجيل بنجاح ويحتوي الرد على معرف المستخدم ورمز الدخول JWT</response>
    /// <response code="400">البيانات غير صالحة أو البريد الإلكتروني مستخدم مسبقاً</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.RegisterAsync(model);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// تسجيل الدخول للمستخدم الموجود
    /// </summary>
    /// <param name="model">بيانات تسجيل الدخول</param>
    /// <remarks>
    /// مثال طلب تسجيل الدخول:
    /// 
    ///     POST /api/auth/login
    ///     {
    ///         "email": "azam@example.com",
    ///         "password": "كلمة_المرور_123"
    ///     }
    /// </remarks>
    /// <response code="200">تم تسجيل الدخول بنجاح ويحتوي الرد على رمز الدخول JWT</response>
    /// <response code="400">البيانات غير صحيحة</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _authService.LoginAsync(model);
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
}