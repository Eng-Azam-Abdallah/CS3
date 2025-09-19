using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RofaqaaApi.DTOs;
using RofaqaaApi.Services;
using System.Security.Claims;

namespace RofaqaaApi.Controllers;

/// <summary>
/// يتحكم في عمليات إدارة المجموعات والأعضاء
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupsController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            throw new InvalidOperationException("User ID not found in claims");
        
        return int.Parse(userIdClaim);
    }

    /// <summary>
    /// إنشاء مجموعة جديدة
    /// </summary>
    /// <param name="model">بيانات المجموعة</param>
    /// <remarks>
    /// مثال الطلب:
    ///
    ///     POST /api/groups
    ///     {
    ///         "name": "سكن الطلاب",
    ///         "description": "مصاريف السكن المشترك"
    ///     }
    /// </remarks>
    /// <response code="200">تم إنشاء المجموعة بنجاح</response>
    /// <response code="400">البيانات غير صالحة</response>
    /// <response code="401">غير مصرح</response>
    [HttpPost]
    [ProducesResponseType(typeof(GroupDto), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<GroupDto>> CreateGroup([FromBody] CreateGroupDto model)
    {
        try
        {
            var group = await _groupService.CreateGroupAsync(GetUserId(), model);
            return Ok(group);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// الحصول على تفاصيل مجموعة محددة
    /// </summary>
    /// <param name="id">معرف المجموعة</param>
    /// <response code="200">تم جلب بيانات المجموعة بنجاح</response>
    /// <response code="404">المجموعة غير موجودة</response>
    /// <response code="401">غير مصرح</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GroupDto), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<GroupDto>> GetGroup(int id)
    {
        var group = await _groupService.GetGroupByIdAsync(id, GetUserId());
        if (group == null)
            return NotFound();

        return Ok(group);
    }

    /// <summary>
    /// الحصول على قائمة مجموعات المستخدم
    /// </summary>
    /// <response code="200">تم جلب قائمة المجموعات بنجاح</response>
    /// <response code="401">غير مصرح</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<GroupDto>), 200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<List<GroupDto>>> GetUserGroups()
    {
        var groups = await _groupService.GetUserGroupsAsync(GetUserId());
        return Ok(groups);
    }

    /// <summary>
    /// تحديث بيانات مجموعة
    /// </summary>
    /// <param name="id">معرف المجموعة</param>
    /// <param name="model">البيانات المحدثة</param>
    /// <remarks>
    /// مثال الطلب:
    ///
    ///     PUT /api/groups/1
    ///     {
    ///         "name": "سكن الطلاب - المبنى الجديد",
    ///         "description": "مصاريف السكن المشترك في المبنى الجديد"
    ///     }
    /// </remarks>
    /// <response code="200">تم تحديث المجموعة بنجاح</response>
    /// <response code="400">البيانات غير صالحة</response>
    /// <response code="401">غير مصرح</response>
    /// <response code="403">ليس لديك صلاحية للتعديل</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(GroupDto), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<GroupDto>> UpdateGroup(int id, [FromBody] UpdateGroupDto model)
    {
        try
        {
            var group = await _groupService.UpdateGroupAsync(id, GetUserId(), model);
            return Ok(group);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// حذف مجموعة
    /// </summary>
    /// <param name="id">معرف المجموعة</param>
    /// <response code="204">تم حذف المجموعة بنجاح</response>
    /// <response code="404">المجموعة غير موجودة</response>
    /// <response code="401">غير مصرح</response>
    /// <response code="403">ليس لديك صلاحية للحذف</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        try
        {
            var result = await _groupService.DeleteGroupAsync(id, GetUserId());
            if (!result)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// إضافة عضو جديد للمجموعة
    /// </summary>
    /// <param name="id">معرف المجموعة</param>
    /// <param name="model">بيانات العضو</param>
    /// <remarks>
    /// مثال الطلب:
    ///
    ///     POST /api/groups/1/members
    ///     {
    ///         "email": "ahmed@example.com",
    ///         "role": "Member"
    ///     }
    /// </remarks>
    /// <response code="200">تم إضافة العضو بنجاح</response>
    /// <response code="400">البيانات غير صالحة</response>
    /// <response code="401">غير مصرح</response>
    /// <response code="403">ليس لديك صلاحية لإضافة أعضاء</response>
    [HttpPost("{id}/members")]
    [ProducesResponseType(typeof(GroupDto), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<GroupDto>> AddMember(int id, [FromBody] AddMemberDto model)
    {
        try
        {
            var group = await _groupService.AddMemberAsync(id, GetUserId(), model);
            return Ok(group);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// تحديث دور عضو في المجموعة
    /// </summary>
    /// <param name="id">معرف المجموعة</param>
    /// <param name="model">بيانات تحديث الدور</param>
    /// <remarks>
    /// مثال الطلب:
    ///
    ///     PUT /api/groups/1/members
    ///     {
    ///         "userId": 2,
    ///         "role": "Admin"
    ///     }
    /// </remarks>
    /// <response code="200">تم تحديث دور العضو بنجاح</response>
    /// <response code="400">البيانات غير صالحة</response>
    /// <response code="401">غير مصرح</response>
    /// <response code="403">ليس لديك صلاحية لتعديل الأدوار</response>
    [HttpPut("{id}/members")]
    [ProducesResponseType(typeof(GroupDto), 200)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<GroupDto>> UpdateMemberRole(int id, [FromBody] UpdateMemberRoleDto model)
    {
        try
        {
            var group = await _groupService.UpdateMemberRoleAsync(id, GetUserId(), model);
            return Ok(group);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// إزالة عضو من المجموعة
    /// </summary>
    /// <param name="id">معرف المجموعة</param>
    /// <param name="userId">معرف العضو</param>
    /// <response code="204">تم إزالة العضو بنجاح</response>
    /// <response code="404">العضو أو المجموعة غير موجودة</response>
    /// <response code="401">غير مصرح</response>
    /// <response code="403">ليس لديك صلاحية لإزالة الأعضاء</response>
    [HttpDelete("{id}/members/{userId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        try
        {
            var result = await _groupService.RemoveMemberAsync(id, GetUserId(), userId);
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