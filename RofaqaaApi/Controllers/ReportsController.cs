using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RofaqaaApi.DTOs;
using RofaqaaApi.Services;
using System.Security.Claims;

namespace RofaqaaApi.Controllers;

[Authorize]
[ApiController]
[Route("api/groups/{groupId}/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly ICalculationService _calculationService;

    public ReportsController(ICalculationService calculationService)
    {
        _calculationService = calculationService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            throw new InvalidOperationException("User ID not found in claims");
        
        return int.Parse(userIdClaim);
    }

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<GroupBalanceSheetDto>> GetGroupBalanceSheet(int groupId)
    {
        try
        {
            var balanceSheet = await _calculationService.GetGroupBalanceSheetAsync(groupId, GetUserId());
            return Ok(balanceSheet);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("member-balance/{memberId}")]
    public async Task<ActionResult<DetailedMemberBalanceDto>> GetDetailedMemberBalance(
        int groupId,
        int memberId)
    {
        try
        {
            var balance = await _calculationService.GetDetailedMemberBalanceAsync(
                groupId,
                memberId,
                GetUserId());
            return Ok(balance);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("settlements")]
    public async Task<ActionResult<List<DebtSettlementDto>>> GetSettlements(int groupId)
    {
        try
        {
            var settlements = await _calculationService.CalculateOptimalSettlementAsync(
                groupId,
                GetUserId());
            return Ok(settlements);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}