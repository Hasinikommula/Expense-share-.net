using ExpenseShareAPI.Data;
using ExpenseShareAPI.DTO_s;
using ExpenseShareAPI.ExpenseShareModels;


namespace ExpenseShareAPI.Services
{
    public interface IGroupService
    {
        public Group AddGroup(CreateGroupDTO group);
        public List<Group> GetAllGroups();
        public void AddUserToGroup(string email, string groupName);

        public List<User> GetUserByGroupId(int id);
    }

    public class GroupService : IGroupService
    {
        private readonly AppDbContext _db;

        public GroupService(AppDbContext db)
        {
            _db = db;
        }
        public Group AddGroup(CreateGroupDTO groupDto)
        {
            if (groupDto == null)
                throw new ArgumentNullException(nameof(groupDto));

            var group = new Group
            {
                Name = groupDto.Name,
                Description = groupDto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _db.Groups.Add(group);
            _db.SaveChanges();

            return group;
        }

        public List<Group> GetAllGroups()
        {
            return _db.Groups.ToList();
        }

        public void AddUserToGroup(string email, string groupName)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == email);
            var group = _db.Groups.FirstOrDefault(g => g.Name == groupName);

            if (group == null || user == null)
                throw new ArgumentException("Group or User not found.");

            // Check if already exists
            var exists = _db.GroupMembers
                .Any(gm => gm.GroupId == group.Id && gm.UserId == user.Id);

            if (exists)
                throw new InvalidOperationException("User already in group.");

            var groupMember = new GroupMember
            {
                GroupId = group.Id,
                UserId = user.Id,
                JoinedAt = DateTime.UtcNow
            };

            _db.GroupMembers.Add(groupMember);
            _db.SaveChanges();
        }

        public List<User> GetUserByGroupId(int id)
        {
            var userList = _db.GroupMembers
                .Where(gm => gm.GroupId == id)
                .Select(gm => gm.User)
                .ToList();

            return userList;
        }
    }
}