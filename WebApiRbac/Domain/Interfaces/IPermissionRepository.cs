using WebApiRbac.Domain.Entities;

namespace WebApiRbac.Domain.Interfaces
{
    public interface IPermissionRepository
    {
        Task<Permission?> GetAllAsync();
    }
}
