using Microsoft.EntityFrameworkCore;
using RofaqaaApi.Models;

namespace RofaqaaApi.Data;

public static class DbSeeder
{
    public static async Task SeedData(ApplicationDbContext context)
    {
        // التحقق من وجود بيانات
        if (await context.Users.AnyAsync())
            return;

        // إضافة المستخدمين
        var users = new List<User>
        {
            new User
            {
                Username = "عزام",
                Email = "azam@example.com",
                PasswordHash = "123456", // في الواقع يجب استخدام تشفير حقيقي
                IsEmailVerified = true
            },
            new User
            {
                Username = "محمد",
                Email = "mohammed@example.com",
                PasswordHash = "123456",
                IsEmailVerified = true
            },
            new User
            {
                Username = "أحمد",
                Email = "ahmed@example.com",
                PasswordHash = "123456",
                IsEmailVerified = true
            },
            new User
            {
                Username = "خالد",
                Email = "khalid@example.com",
                PasswordHash = "123456",
                IsEmailVerified = true
            }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        // إنشاء مجموعة
        var group = new Group
        {
            Name = "سكن الطلاب",
            Description = "مصاريف السكن المشترك"
        };

        context.Groups.Add(group);
        await context.SaveChangesAsync();

        // إضافة الأعضاء للمجموعة
        var groupMembers = new List<GroupMember>();
        for (var i = 0; i < users.Count; i++)
        {
            groupMembers.Add(new GroupMember
            {
                GroupId = group.Id,
                UserId = users[i].Id,
                Role = i == 0 ? GroupRole.Owner : GroupRole.Member
            });
        }

        context.GroupMembers.AddRange(groupMembers);
        await context.SaveChangesAsync();

        // إضافة بعض المصاريف
        var expenses = new List<Expense>
        {
            new Expense
            {
                GroupId = group.Id,
                Description = "مشتريات البقالة الأسبوعية",
                Amount = 500,
                Category = ExpenseCategory.Food,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Expense
            {
                GroupId = group.Id,
                Description = "فاتورة الكهرباء",
                Amount = 200,
                Category = ExpenseCategory.Utilities,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new Expense
            {
                GroupId = group.Id,
                Description = "وجبة عشاء مشتركة",
                Amount = 300,
                Category = ExpenseCategory.Food,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        context.Expenses.AddRange(expenses);
        await context.SaveChangesAsync();

        // إضافة الدفعات والحصص
        // المصروف الأول: عزام دفع الكل، والحصص متساوية
        var expense1Payments = new List<Payment>
        {
            new Payment
            {
                ExpenseId = expenses[0].Id,
                GroupMemberId = groupMembers[0].Id,
                Amount = 500
            }
        };

        var expense1Shares = groupMembers.Select(m => new ExpenseShare
        {
            ExpenseId = expenses[0].Id,
            GroupMemberId = m.Id,
            Type = ShareType.Equal,
            Amount = 125
        }).ToList();

        // المصروف الثاني: محمد وأحمد دفعوا، والحصص متساوية
        var expense2Payments = new List<Payment>
        {
            new Payment
            {
                ExpenseId = expenses[1].Id,
                GroupMemberId = groupMembers[1].Id,
                Amount = 100
            },
            new Payment
            {
                ExpenseId = expenses[1].Id,
                GroupMemberId = groupMembers[2].Id,
                Amount = 100
            }
        };

        var expense2Shares = groupMembers.Select(m => new ExpenseShare
        {
            ExpenseId = expenses[1].Id,
            GroupMemberId = m.Id,
            Type = ShareType.Equal,
            Amount = 50
        }).ToList();

        // المصروف الثالث: خالد دفع، والحصص بالنسب
        var expense3Payments = new List<Payment>
        {
            new Payment
            {
                ExpenseId = expenses[2].Id,
                GroupMemberId = groupMembers[3].Id,
                Amount = 300
            }
        };

        var expense3Shares = new List<ExpenseShare>
        {
            new ExpenseShare
            {
                ExpenseId = expenses[2].Id,
                GroupMemberId = groupMembers[0].Id,
                Type = ShareType.Percentage,
                Percentage = 30,
                Amount = 90
            },
            new ExpenseShare
            {
                ExpenseId = expenses[2].Id,
                GroupMemberId = groupMembers[1].Id,
                Type = ShareType.Percentage,
                Percentage = 20,
                Amount = 60
            },
            new ExpenseShare
            {
                ExpenseId = expenses[2].Id,
                GroupMemberId = groupMembers[2].Id,
                Type = ShareType.Percentage,
                Percentage = 25,
                Amount = 75
            },
            new ExpenseShare
            {
                ExpenseId = expenses[2].Id,
                GroupMemberId = groupMembers[3].Id,
                Type = ShareType.Percentage,
                Percentage = 25,
                Amount = 75
            }
        };

        context.Payments.AddRange(expense1Payments);
        context.Payments.AddRange(expense2Payments);
        context.Payments.AddRange(expense3Payments);

        context.ExpenseShares.AddRange(expense1Shares);
        context.ExpenseShares.AddRange(expense2Shares);
        context.ExpenseShares.AddRange(expense3Shares);

        await context.SaveChangesAsync();
    }
}