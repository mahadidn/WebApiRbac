using WebApiRbac.Domain.Interfaces;
using WebApiRbac.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using WebApiRbac.Domain.Entities;

namespace WebApiRbac.Infrastructure.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly ApplicationDbContext _context;

        public PermissionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Permission>> GetAllAsync()
        {
            // AsNoTracking() membuat query jauh lebih cepat dan hemat RAM
            // karena EF Core tidak perlu melacak perubahan pada data ini.
            return await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Name) // urutkan berdasarkan abjad
                .ToListAsync();

        }


    }
}
