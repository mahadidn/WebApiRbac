using WebApiRbac.Domain.Entities;

namespace WebApiRbac.Domain.Interfaces
{
    public interface IUserRepository
    {
        // Task<> is the C# version of asynchronous
        Task<User?> GetByIdAsync(Guid id); // get user by id
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);

        // user management
        Task AddAsync(User user);
        Task UpdateAsync(User user);

        // role-user management
        Task AddRolesAsync(Guid userId, IEnumerable<Guid> roleIds);
        Task RemoveRolesAsync(Guid userId, IEnumerable<Guid> roleIds);
        Task SyncRolesAsync(Guid userId, IEnumerable<Guid> roleIds);

        // Retrieve all permission names from a user
        Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId);
        // Retrieve all permission names from a user
        Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId);
    }
}
