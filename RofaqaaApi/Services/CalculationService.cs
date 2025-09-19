using Microsoft.EntityFrameworkCore;
using RofaqaaApi.Data;
using RofaqaaApi.DTOs;

namespace RofaqaaApi.Services;

public interface ICalculationService
{
    Task<GroupBalanceSheetDto> GetGroupBalanceSheetAsync(int groupId, int userId);
    Task<DetailedMemberBalanceDto> GetDetailedMemberBalanceAsync(int groupId, int memberId, int requestingUserId);
    Task<List<DebtSettlementDto>> CalculateOptimalSettlementAsync(int groupId, int userId);
}

public class CalculationService : ICalculationService
{
    private readonly ApplicationDbContext _context;

    public CalculationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GroupBalanceSheetDto> GetGroupBalanceSheetAsync(int groupId, int userId)
    {
        // التحقق من عضوية المستخدم في المجموعة
        var isMember = await _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (!isMember)
            throw new InvalidOperationException("User is not a member of this group");

        // جلب معلومات المجموعة
        var group = await _context.Groups
            .Include(g => g.Members)
                .ThenInclude(m => m.User)
            .Include(g => g.Expenses)
                .ThenInclude(e => e.Shares)
            .Include(g => g.Expenses)
                .ThenInclude(e => e.Payments)
            .FirstOrDefaultAsync(g => g.Id == groupId)
            ?? throw new InvalidOperationException("Group not found");

        var totalExpenses = group.Expenses.Sum(e => e.Amount);
        var memberBalances = new List<MemberBalanceDto>();

        foreach (var member in group.Members)
        {
            // حساب إجمالي المدفوعات
            var totalPaid = group.Expenses
                .SelectMany(e => e.Payments)
                .Where(p => p.GroupMemberId == member.Id)
                .Sum(p => p.Amount);

            // حساب إجمالي الحصص
            var totalShare = group.Expenses
                .SelectMany(e => e.Shares)
                .Where(s => s.GroupMemberId == member.Id)
                .Sum(s => s.Amount);

            // إنشاء التفاصيل لكل مصروف
            var breakdown = new List<ExpenseBreakdownDto>();
            foreach (var expense in group.Expenses)
            {
                var paid = expense.Payments
                    .Where(p => p.GroupMemberId == member.Id)
                    .Sum(p => p.Amount);

                var share = expense.Shares
                    .FirstOrDefault(s => s.GroupMemberId == member.Id);

                if (paid > 0 || share != null)
                {
                    breakdown.Add(new ExpenseBreakdownDto
                    {
                        ExpenseId = expense.Id,
                        Description = expense.Description,
                        Date = expense.CreatedAt,
                        TotalAmount = expense.Amount,
                        PaidAmount = paid,
                        ShareAmount = share?.Amount ?? 0,
                        ShareType = share?.Type.ToString() ?? "None",
                        SharePercentage = share?.Percentage
                    });
                }
            }

            memberBalances.Add(new MemberBalanceDto
            {
                UserId = member.UserId,
                Username = member.User.Username,
                TotalPaid = totalPaid,
                TotalShare = totalShare,
                NetBalance = totalPaid - totalShare,
                Breakdown = breakdown
            });
        }

        // حساب التسويات المثالية
        var settlements = await CalculateOptimalSettlementAsync(groupId, userId);

        return new GroupBalanceSheetDto
        {
            GroupId = group.Id,
            GroupName = group.Name,
            TotalExpenses = totalExpenses,
            MemberBalances = memberBalances,
            Settlements = settlements
        };
    }

    public async Task<DetailedMemberBalanceDto> GetDetailedMemberBalanceAsync(
        int groupId, 
        int memberId, 
        int requestingUserId)
    {
        // التحقق من عضوية المستخدم الطالب في المجموعة
        var requesterMembership = await _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == requestingUserId);

        if (!requesterMembership)
            throw new InvalidOperationException("User is not a member of this group");

        // جلب معلومات العضو المطلوب
        var member = await _context.GroupMembers
            .Include(m => m.User)
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == memberId)
            ?? throw new InvalidOperationException("Member not found");

        var transactions = new List<DetailedTransactionDto>();

        // جلب جميع المصاريف المتعلقة بالعضو
        var expenses = await _context.Expenses
            .Include(e => e.Shares)
                .ThenInclude(s => s.GroupMember)
                    .ThenInclude(m => m.User)
            .Include(e => e.Payments)
                .ThenInclude(p => p.GroupMember)
                    .ThenInclude(m => m.User)
            .Where(e => e.GroupId == groupId &&
                (e.Payments.Any(p => p.GroupMemberId == member.Id) ||
                 e.Shares.Any(s => s.GroupMemberId == member.Id)))
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        foreach (var expense in expenses)
        {
            var payment = expense.Payments
                .FirstOrDefault(p => p.GroupMemberId == member.Id);

            var share = expense.Shares
                .FirstOrDefault(s => s.GroupMemberId == member.Id);

            // إذا دفع شيئاً
            if (payment != null)
            {
                var otherPayers = expense.Payments
                    .Where(p => p.GroupMemberId != member.Id)
                    .Select(p => p.GroupMember.User.Username)
                    .ToList();

                transactions.Add(new DetailedTransactionDto
                {
                    ExpenseId = expense.Id,
                    Description = expense.Description,
                    Category = expense.Category.ToString(),
                    Date = payment.PaidAt,
                    Amount = payment.Amount,
                    Type = "Payment",
                    OtherParticipants = otherPayers
                });
            }

            // إذا كان عليه حصة
            if (share != null)
            {
                var otherShares = expense.Shares
                    .Where(s => s.GroupMemberId != member.Id)
                    .Select(s => s.GroupMember.User.Username)
                    .ToList();

                transactions.Add(new DetailedTransactionDto
                {
                    ExpenseId = expense.Id,
                    Description = expense.Description,
                    Category = expense.Category.ToString(),
                    Date = expense.CreatedAt,
                    Amount = share.Amount,
                    Type = "Share",
                    ShareType = share.Type.ToString(),
                    Percentage = share.Percentage,
                    OtherParticipants = otherShares
                });
            }
        }

        // حساب الأرصدة الإجمالية
        var totalPaid = expenses
            .SelectMany(e => e.Payments)
            .Where(p => p.GroupMemberId == member.Id)
            .Sum(p => p.Amount);

        var totalShare = expenses
            .SelectMany(e => e.Shares)
            .Where(s => s.GroupMemberId == member.Id)
            .Sum(s => s.Amount);

        return new DetailedMemberBalanceDto
        {
            Balance = new MemberBalanceDto
            {
                UserId = member.UserId,
                Username = member.User.Username,
                TotalPaid = totalPaid,
                TotalShare = totalShare,
                NetBalance = totalPaid - totalShare
            },
            Transactions = transactions
        };
    }

    public async Task<List<DebtSettlementDto>> CalculateOptimalSettlementAsync(int groupId, int userId)
    {
        // التحقق من عضوية المستخدم في المجموعة
        var isMember = await _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (!isMember)
            throw new InvalidOperationException("User is not a member of this group");

        // حساب الأرصدة لجميع الأعضاء
        var members = await _context.GroupMembers
            .Include(m => m.User)
            .Where(m => m.GroupId == groupId)
            .ToListAsync();

        var balances = new Dictionary<int, (decimal Amount, string Username)>();
        foreach (var member in members)
        {
            var paid = await _context.Payments
                .Where(p => p.GroupMemberId == member.Id)
                .SumAsync(p => p.Amount);

            var share = await _context.ExpenseShares
                .Where(s => s.GroupMemberId == member.Id)
                .SumAsync(s => s.Amount);

            balances[member.UserId] = (paid - share, member.User.Username);
        }

        var settlements = new List<DebtSettlementDto>();
        var debtors = balances.Where(b => b.Value.Amount < 0)
            .OrderBy(b => b.Value.Amount)
            .ToList();
        var creditors = balances.Where(b => b.Value.Amount > 0)
            .OrderByDescending(b => b.Value.Amount)
            .ToList();

        foreach (var debtor in debtors)
        {
            var remainingDebt = Math.Abs(debtor.Value.Amount);
            foreach (var creditor in creditors.Where(c => c.Value.Amount > 0))
            {
                if (remainingDebt == 0) break;

                var settlementAmount = Math.Min(remainingDebt, creditor.Value.Amount);
                if (settlementAmount > 0)
                {
                    settlements.Add(new DebtSettlementDto
                    {
                        FromUserId = debtor.Key,
                        FromUsername = debtor.Value.Username,
                        ToUserId = creditor.Key,
                        ToUsername = creditor.Value.Username,
                        Amount = settlementAmount
                    });

                    remainingDebt -= settlementAmount;
                    balances[creditor.Key] = (creditor.Value.Amount - settlementAmount, creditor.Value.Username);
                }
            }
        }

        return settlements;
    }
}