using ExpenseShareAPI.Data;
using ExpenseShareAPI.DTO_s;
using ExpenseShareAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseShareAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   
        public class GroupController : ControllerBase
        {
            private readonly IGroupService _groupService;
            // Changed type from Group to your DbContext (assumed name: ExpenseSharingDbContext)
            private readonly AppDbContext db;

            // Changed constructor parameter from Group to ExpenseSharingDbContext
            public GroupController(IGroupService groupService, AppDbContext db)
            {
                _groupService = groupService;
                this.db = db;
            }
            [HttpPost("CreateGroup")]
            public IActionResult CreateGroup([FromBody] CreateGroupDTO groupDto)
            {
                if (groupDto == null)
                {
                    return BadRequest("Group data is required.");
                }
                var group = _groupService.AddGroup(groupDto);
                return Ok(group);
            }
            [HttpGet("Getallgroups")]
            public IActionResult GetAllGroups()
            {
                var groupsList = _groupService.GetAllGroups();
                if (groupsList == null)
                {
                    return BadRequest("No groups are available");
                }
                return Ok(groupsList);
            }

            [HttpPost("AddUserToGroup")]
            public IActionResult AddUserToGroup([FromBody] AssigningUserDTO dto)
            {
                if (dto == null)
                {
                    return BadRequest("Data is required.");
                }
                try
                {
                    _groupService.AddUserToGroup(dto.email, dto.GroupName);
                    return Ok("User added to group successfully.");
                }
                catch (ArgumentException ex)
                {
                    return NotFound(ex.Message);
                }
                catch (Exception)
                {
                    return StatusCode(500, "An error occurred while adding the user to the group.");
                }
            }

            [HttpGet("GetGroupMembers/{id}")]
            public IActionResult GetGroupMembers(int id)
            {
                var userList = _groupService.GetUserByGroupId(id);
                return Ok(userList);
            }
        }
    }

