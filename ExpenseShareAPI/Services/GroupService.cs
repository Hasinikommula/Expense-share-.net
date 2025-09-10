using ExpenseShareAPI.Data;
using ExpenseShareAPI.DTO_s;
using ExpenseShareAPI.ExpenseShareModels;
using Microsoft.EntityFrameworkCore;


namespace ExpenseShareAPI.Services
{
    public interface IGroupService
    {
        Task<Group> AddGroupAsync(CreateGroupDTO group);
        Task<List<GroupSummaryDto>> GetAllGroupsAsync(); 
        Task AddUserToGroupAsync(string email, string groupName);
        Task<List<User>> GetUserByGroupIdAsync(int id);
        Task<object> GetGroupDetailsAsync(int id); 
        Task<List<object>> GetAssignmentsAsync();  
    }

    public class GroupService : IGroupService
    {
        private readonly AppDbContext _db;

        public GroupService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Group> AddGroupAsync(CreateGroupDTO groupDto)
        {
            if (groupDto == null)
                throw new ArgumentNullException(nameof(groupDto));

            var group = new Group
            {
                Name = groupDto.Name,
                Description = groupDto.Description,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Groups.AddAsync(group);
            await _db.SaveChangesAsync();
            return group;
        }

        public async Task<List<GroupSummaryDto>> GetAllGroupsAsync()
        {
            // ✅ Efficiently gets groups and their member counts in one query
            return await _db.Groups
                .Select(g => new GroupSummaryDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    MemberCount = g.GroupMembers.Count()
                })
                .ToListAsync();
        }

        public async Task AddUserToGroupAsync(string email, string groupName)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            var group = await _db.Groups.FirstOrDefaultAsync(g => g.Name == groupName);

            if (group == null || user == null)
                throw new ArgumentException("Group or User not found.");

            var exists = await _db.GroupMembers
                .AnyAsync(gm => gm.GroupId == group.Id && gm.UserId == user.Id);

            if (exists)
                throw new InvalidOperationException("User already in group.");

            var groupMember = new GroupMember
            {
                GroupId = group.Id,
                UserId = user.Id,
                JoinedAt = DateTime.UtcNow
            };

            await _db.GroupMembers.AddAsync(groupMember);
            await _db.SaveChangesAsync();
        }

        public async Task<List<User>> GetUserByGroupIdAsync(int id)
        {
            // ✅ Uses .Include() to efficiently load related User data
            return await _db.GroupMembers
                .Where(gm => gm.GroupId == id)
                .Include(gm => gm.User)
                .Select(gm => gm.User)
                .ToListAsync();
        }

        // ✅ Logic moved from controller
        public async Task<object> GetGroupDetailsAsync(int id)
        {
            var group = await _db.Groups
                .Include(g => g.GroupMembers)
                    .ThenInclude(gm => gm.User)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return null;

            return new
            {
                group.Id,
                group.Name,
                group.Description,
                Members = group.GroupMembers.Select(gm => new { gm.User.Id, gm.User.Username }).ToList()
            };
        }

        // ✅ Logic moved from controller
        public async Task<List<object>> GetAssignmentsAsync()
        {
            return await _db.GroupMembers
                .Include(ug => ug.User)
                .Include(ug => ug.Group)
                .Select(ug => new
                {
                    UserEmail = ug.User.Email,
                    GroupName = ug.Group.Name
                })
                .ToListAsync<object>();
        }
    }
}