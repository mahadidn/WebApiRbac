using WebApiRbac.Application.DTOs;
using WebApiRbac.Application.DTOs.Users;

namespace WebApiRbac.Application.Interfaces
{
    public interface IUserService
    {
        // ambil semua user
        //Task<IEnumerable<UsersResponseDto>> GetAllUsersAsync();
        Task<PagedResponseDto<UsersResponseDto>> GetAllUsersAsync(int pageNumber, int pageSize);

        // ambil 1 user beserta detail role-nya
        Task<UserResponseDto?> GetUserByIdAsync(Guid id);

        // update data dasar user (username/email)
        Task<UserResponseDto> UpdateUserAsync(Guid id, UserUpdateDto request);

        // sinkronisasi role user
        Task SyncUserRolesAsync(Guid userId, SyncUserRolesRequestDto request);

    }
}
