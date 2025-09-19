using Microsoft.EntityFrameworkCore;
using RofaqaaApi.Data;
using RofaqaaApi.DTOs;
using RofaqaaApi.Models;

namespace RofaqaaApi.Services;

public interface IGroupService
{
    Task<GroupDto> CreateGroupAsync(int userId, CreateGroupDto model);
    Task<GroupDto?> GetGroupByIdAsync(int groupId, int userId);
    Task<List<GroupDto>> GetUserGroupsAsync(int userId);
    Task<GroupDto> UpdateGroupAsync(int groupId, int userId, UpdateGroupDto model);
    Task<bool> DeleteGroupAsync(int groupId, int userId);
    Task<GroupDto> AddMemberAsync(int groupId, int adminUserId, AddMemberDto model);
    Task<GroupDto> UpdateMemberRoleAsync(int groupId, int adminUserId, UpdateMemberRoleDto model);
    Task<bool> RemoveMemberAsync(int groupId, int adminUserId, int memberUserId);
}

public class GroupService : IGroupService
{
    private readonly ApplicationDbContext _context;

    public GroupService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GroupDto> CreateGroupAsync(int userId, CreateGroupDto model)
    {
        var group = new Group
        {
            Name = model.Name,
            Description = model.Description
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // إضافة المستخدم كمالك للمجموعة
        var member = new GroupMember
        {
            UserId = userId,
            GroupId = group.Id,
            Role = GroupRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        _context.GroupMembers.Add(member);
        await _context.SaveChangesAsync();

        return await GetGroupByIdAsync(group.Id, userId) 
            ?? throw new InvalidOperationException("Failed to create group");
    }

    public async Task<GroupDto?> GetGroupByIdAsync(int groupId, int userId)
    {
        var group = await _context.Groups
            .Include(g => g.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null) return null;

        // التحقق من أن المستخدم عضو في المجموعة
        var isMember = await _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (!isMember) return null;

        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            CreatedAt = group.CreatedAt,
            Members = group.Members.Select(m => new GroupMemberDto
            {
                Id = m.Id,
                UserId = m.UserId,
                Username = m.User.Username,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt
            }).ToList()
        };
    }

    public async Task<List<GroupDto>> GetUserGroupsAsync(int userId)
    {
        var memberGroups = await _context.GroupMembers
            .Include(m => m.Group)
            .ThenInclude(g => g.Members)
            .ThenInclude(m => m.User)
            .Where(m => m.UserId == userId)
            .Select(m => m.Group)
            .ToListAsync();

        return memberGroups.Select(g => new GroupDto
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            CreatedAt = g.CreatedAt,
            Members = g.Members.Select(m => new GroupMemberDto
            {
                Id = m.Id,
                UserId = m.UserId,
                Username = m.User.Username,
                Role = m.Role.ToString(),
                JoinedAt = m.JoinedAt
            }).ToList()
        }).ToList();
    }

    public async Task<GroupDto> UpdateGroupAsync(int groupId, int userId, UpdateGroupDto model)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null)
            throw new InvalidOperationException("Group not found");

        // التحقق من أن المستخدم هو مالك أو مدير للمجموعة
        var userRole = await _context.GroupMembers
            .Where(m => m.GroupId == groupId && m.UserId == userId)
            .Select(m => m.Role)
            .FirstOrDefaultAsync();

        if (userRole != GroupRole.Owner && userRole != GroupRole.Admin)
            throw new InvalidOperationException("Unauthorized");

        group.Name = model.Name;
        group.Description = model.Description;

        await _context.SaveChangesAsync();

        return await GetGroupByIdAsync(groupId, userId) 
            ?? throw new InvalidOperationException("Failed to update group");
    }

    public async Task<bool> DeleteGroupAsync(int groupId, int userId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null)
            return false;

        // التحقق من أن المستخدم هو مالك للمجموعة
        var isOwner = await _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId && m.Role == GroupRole.Owner);

        if (!isOwner)
            throw new InvalidOperationException("Only the owner can delete the group");

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<GroupDto> AddMemberAsync(int groupId, int adminUserId, AddMemberDto model)
    {
        // التحقق من صلاحيات المستخدم
        var adminRole = await _context.GroupMembers
            .Where(m => m.GroupId == groupId && m.UserId == adminUserId)
            .Select(m => m.Role)
            .FirstOrDefaultAsync();

        if (adminRole != GroupRole.Owner && adminRole != GroupRole.Admin)
            throw new InvalidOperationException("Unauthorized");

        // البحث عن المستخدم المراد إضافته
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null)
            throw new InvalidOperationException("User not found");

        // التحقق من أن المستخدم ليس عضواً بالفعل
        var existingMember = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == user.Id);

        if (existingMember != null)
            throw new InvalidOperationException("User is already a member");

        var role = Enum.Parse<GroupRole>(model.Role);

        // التحقق من أن المدير لا يمكنه إضافة مالك
        if (role == GroupRole.Owner && adminRole != GroupRole.Owner)
            throw new InvalidOperationException("Only the owner can add another owner");

        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = user.Id,
            Role = role
        };

        _context.GroupMembers.Add(member);
        await _context.SaveChangesAsync();

        return await GetGroupByIdAsync(groupId, adminUserId) 
            ?? throw new InvalidOperationException("Failed to add member");
    }

    public async Task<GroupDto> UpdateMemberRoleAsync(int groupId, int adminUserId, UpdateMemberRoleDto model)
    {
        // التحقق من صلاحيات المستخدم
        var adminRole = await _context.GroupMembers
            .Where(m => m.GroupId == groupId && m.UserId == adminUserId)
            .Select(m => m.Role)
            .FirstOrDefaultAsync();

        if (adminRole != GroupRole.Owner && adminRole != GroupRole.Admin)
            throw new InvalidOperationException("Unauthorized");

        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == model.UserId);

        if (member == null)
            throw new InvalidOperationException("Member not found");

        var newRole = Enum.Parse<GroupRole>(model.Role);

        // التحقق من القيود على تغيير الأدوار
        if (member.Role == GroupRole.Owner && adminRole != GroupRole.Owner)
            throw new InvalidOperationException("Cannot change owner's role");

        if (newRole == GroupRole.Owner && adminRole != GroupRole.Owner)
            throw new InvalidOperationException("Only the owner can assign owner role");

        member.Role = newRole;
        await _context.SaveChangesAsync();

        return await GetGroupByIdAsync(groupId, adminUserId) 
            ?? throw new InvalidOperationException("Failed to update member role");
    }

    public async Task<bool> RemoveMemberAsync(int groupId, int adminUserId, int memberUserId)
    {
        // التحقق من صلاحيات المستخدم
        var adminRole = await _context.GroupMembers
            .Where(m => m.GroupId == groupId && m.UserId == adminUserId)
            .Select(m => m.Role)
            .FirstOrDefaultAsync();

        if (adminRole != GroupRole.Owner && adminRole != GroupRole.Admin)
            throw new InvalidOperationException("Unauthorized");

        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == memberUserId);

        if (member == null)
            return false;

        // التحقق من القيود على إزالة الأعضاء
        if (member.Role == GroupRole.Owner)
            throw new InvalidOperationException("Cannot remove the owner");

        if (member.Role == GroupRole.Admin && adminRole != GroupRole.Owner)
            throw new InvalidOperationException("Only the owner can remove admins");

        _context.GroupMembers.Remove(member);
        await _context.SaveChangesAsync();

        return true;
    }
}