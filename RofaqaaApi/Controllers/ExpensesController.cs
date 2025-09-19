using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RofaqaaApi.DTOs;
using RofaqaaApi.Services;
using System.Security.Claims;

namespace RofaqaaApi.Controllers;

[Authorize]
[ApiController]
[Route("api/groups/{groupId}/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            throw new InvalidOperationException("User ID not found in claims");
        
        return int.Parse(userIdClaim);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseDetailsDto>> CreateExpense(
        int groupId,
        [FromBody] CreateExpenseDto model)
    {
        try
        {
            var expense = await _expenseService.CreateExpenseAsync(groupId, GetUserId(), model);
            return Ok(expense);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExpenseDetailsDto>> GetExpense(
        int groupId,
        int id)
    {
        var expense = await _expenseService.GetExpenseByIdAsync(id, GetUserId());
        if (expense == null)
            return NotFound();

        return Ok(expense);
    }

    [HttpGet]
    public async Task<ActionResult<List<ExpenseSummaryDto>>> GetGroupExpenses(
        int groupId)
    {
        try
        {
            var expenses = await _expenseService.GetGroupExpensesAsync(groupId, GetUserId());
            return Ok(expenses);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(
        int groupId,
        int id)
    {
        try
        {
            var result = await _expenseService.DeleteExpenseAsync(id, GetUserId());
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}