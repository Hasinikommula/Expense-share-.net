using ExpenseShareAPI.Data;
using ExpenseShareAPI.DTO_s;
using ExpenseShareAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseShareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;

       
        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost("CreateGroup")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDTO groupDto)
        {
            if (groupDto == null)
            {
                return BadRequest("Group data is required.");
            }
            var group = await _groupService.AddGroupAsync(groupDto);
            return Ok(group);
        }

        [HttpGet("Getallgroups")]
        public async Task<IActionResult> GetAllGroups()
        {
            var groupsList = await _groupService.GetAllGroupsAsync();
            return Ok(groupsList);
        }

        [HttpPost("AddUserToGroup")]
        public async Task<IActionResult> AddUserToGroup([FromBody] AssigningUserDTO dto)
        {
            if (dto == null)
            {
                return BadRequest("Data is required.");
            }
            try
            {
                await _groupService.AddUserToGroupAsync(dto.email, dto.GroupName);
                return Ok(new { message = "User added to group successfully." });
            }
            catch (Exception ex)
            {
                
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("assignments")]
        public async Task<IActionResult> GetAssignments()
        {
            var assignments = await _groupService.GetAssignmentsAsync();
            return Ok(assignments);
        }

        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetGroupDetails(int id)
        {
            var groupDetails = await _groupService.GetGroupDetailsAsync(id);
            if (groupDetails == null)
            {
                return NotFound();
            }
            return Ok(groupDetails);
        }

        [HttpGet("GetGroupMembers/{id}")]
        public async Task<IActionResult> GetGroupMembers(int id)
        {
            var userList = await _groupService.GetUserByGroupIdAsync(id);
            return Ok(userList);
        }
    }
}

