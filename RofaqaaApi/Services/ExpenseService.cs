using Microsoft.EntityFrameworkCore;
using RofaqaaApi.Data;
using RofaqaaApi.DTOs;
using RofaqaaApi.Models;

namespace RofaqaaApi.Services;

public interface IExpenseService
{
    Task<ExpenseDetailsDto> CreateExpenseAsync(int groupId, int userId, CreateExpenseDto model);
    Task<ExpenseDetailsDto?> GetExpenseByIdAsync(int expenseId, int userId);
    Task<List<ExpenseSummaryDto>> GetGroupExpensesAsync(int groupId, int userId);
    Task<bool> DeleteExpenseAsync(int expenseId, int userId);
}

public class ExpenseService : IExpenseService
{
    private readonly ApplicationDbContext _context;

    public ExpenseService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExpenseDetailsDto> CreateExpenseAsync(int groupId, int userId, CreateExpenseDto model)
    {
        // التحقق من عضوية المستخدم في المجموعة
        var userMembership = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (userMembership == null)
            throw new InvalidOperationException("User is not a member of this group");

        // التحقق من صحة المبالغ المدفوعة
        var totalPayments = model.Payments.Sum(p => p.Amount);
        if (totalPayments != model.Amount)
            throw new InvalidOperationException("مجموع المبالغ المدفوعة يجب أن يساوي المبلغ الإجمالي");

        // إنشاء المصروف
        var expense = new Expense
        {
            GroupId = groupId,
            Description = model.Description,
            Amount = model.Amount,
            Category = Enum.Parse<ExpenseCategory>(model.Category),
            CreatedAt = DateTime.UtcNow
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();

        // إضافة الحصص
        foreach (var shareDto in model.Shares)
        {
            var share = new ExpenseShare
            {
                ExpenseId = expense.Id,
                GroupMemberId = shareDto.GroupMemberId,
                Type = Enum.Parse<ShareType>(shareDto.Type)
            };

            switch (share.Type)
            {
                case ShareType.Fixed:
                    if (!shareDto.Amount.HasValue)
                        throw new InvalidOperationException("المبلغ مطلوب للحصة الثابتة");
                    share.Amount = shareDto.Amount.Value;
                    break;

                case ShareType.Percentage:
                    if (!shareDto.Percentage.HasValue)
                        throw new InvalidOperationException("النسبة المئوية مطلوبة للحصة النسبية");
                    share.Percentage = shareDto.Percentage.Value;
                    share.Amount = expense.Amount * (shareDto.Percentage.Value / 100);
                    break;

                case ShareType.Equal:
                    share.Amount = expense.Amount / model.Shares.Count;
                    break;
            }

            _context.ExpenseShares.Add(share);
        }

        // إضافة المدفوعات
        foreach (var paymentDto in model.Payments)
        {
            var payment = new Payment
            {
                ExpenseId = expense.Id,
                GroupMemberId = paymentDto.GroupMemberId,
                Amount = paymentDto.Amount,
                PaidAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
        }

        await _context.SaveChangesAsync();

        return await GetExpenseByIdAsync(expense.Id, userId)
            ?? throw new InvalidOperationException("Failed to create expense");
    }

    public async Task<ExpenseDetailsDto?> GetExpenseByIdAsync(int expenseId, int userId)
    {
        var expense = await _context.Expenses
            .Include(e => e.Group)
                .ThenInclude(g => g.Members)
                    .ThenInclude(m => m.User)
            .Include(e => e.Shares)
            .Include(e => e.Payments)
            .FirstOrDefaultAsync(e => e.Id == expenseId);

        if (expense == null)
            return null;

        // التحقق من أن المستخدم عضو في المجموعة
        var isMember = await _context.GroupMembers
            .AnyAsync(m => m.GroupId == expense.GroupId && m.UserId == userId);

        if (!isMember)
            return null;

        return new ExpenseDetailsDto
        {
            Id = expense.Id,
            Description = expense.Description,
            Amount = expense.Amount,
            Category = expense.Category.ToString(),
            CreatedAt = expense.CreatedAt,
            Shares = expense.Shares.Select(s => new ShareDetailDto
            {
                Id = s.Id,
                GroupMemberId = s.GroupMemberId,
                MemberName = s.GroupMember.User.Username,
                Type = s.Type.ToString(),
                Amount = s.Amount,
                Percentage = s.Percentage
            }).ToList(),
            Payments = expense.Payments.Select(p => new PaymentDetailDto
            {
                Id = p.Id,
                GroupMemberId = p.GroupMemberId,
                MemberName = p.GroupMember.User.Username,
                Amount = p.Amount,
                PaidAt = p.PaidAt
            }).ToList()
        };
    }

    public async Task<List<ExpenseSummaryDto>> GetGroupExpensesAsync(int groupId, int userId)
    {
        // التحقق من عضوية المستخدم في المجموعة
        var isMember = await _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (!isMember)
            throw new InvalidOperationException("User is not a member of this group");

        var expenses = await _context.Expenses
            .Include(e => e.Shares)
            .Include(e => e.Payments)
            .Where(e => e.GroupId == groupId)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new ExpenseSummaryDto
            {
                Id = e.Id,
                Description = e.Description,
                Amount = e.Amount,
                Category = e.Category.ToString(),
                CreatedAt = e.CreatedAt,
                PayersCount = e.Payments.Select(p => p.GroupMemberId).Distinct().Count(),
                SharesCount = e.Shares.Count
            })
            .ToListAsync();

        return expenses;
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId, int userId)
    {
        var expense = await _context.Expenses
            .Include(e => e.Group)
                .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(e => e.Id == expenseId);

        if (expense == null)
            return false;

        // التحقق من صلاحيات المستخدم (مالك أو مدير المجموعة)
        var userRole = await _context.GroupMembers
            .Where(m => m.GroupId == expense.GroupId && m.UserId == userId)
            .Select(m => m.Role)
            .FirstOrDefaultAsync();

        if (userRole != GroupRole.Owner && userRole != GroupRole.Admin)
            throw new InvalidOperationException("غير مصرح لك بحذف المصروف");

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();

        return true;
    }
}