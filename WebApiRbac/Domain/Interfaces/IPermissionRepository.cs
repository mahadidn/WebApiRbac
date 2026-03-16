using WebApiRbac.Domain.Entities;

namespace WebApiRbac.Domain.Interfaces
{
    public interface IPermissionRepository
    {
        Task<IEnumerable<Permission>> GetAllAsync();
    }
}
