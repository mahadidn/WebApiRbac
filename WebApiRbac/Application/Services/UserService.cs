using WebApiRbac.Application.DTOs;
using WebApiRbac.Application.DTOs.Users;
using WebApiRbac.Application.Interfaces;
using WebApiRbac.Domain.Entities;
using WebApiRbac.Domain.Interfaces;

namespace WebApiRbac.Application.Services
{
    public class UserService : IUserService
    {

        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // ambil semua user
        public async Task<PagedResponseDto<UsersResponseDto>> GetAllUsersAsync(int pageNumber, int pageSize)
        {
            // ambil ke repository
            var (users, totalCount) = await _userRepository.GetAllAsync(pageNumber, pageSize);

            // parsing entity ke DTO
            var mappedUsers = users.Select(u => new UsersResponseDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Roles = u.Roles.Select(r => r.Name).ToList()
                
            });

            // bungkus dengan wadah pagination
            return new PagedResponseDto<UsersResponseDto>
            {
                Data = mappedUsers,
                TotalCount = totalCount,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };
        }

        // ambil 1 user beserta detail role-nya
        public async Task<UserResponseDto?> GetUserByIdAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return null;

            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = user.Roles.Select(r => new UserRoleDto
                {
                    Id = r.Id,
                    Name = r.Name
                }).ToList()
            };

        }

        // update data dasar user (username/email)
        public async Task<UserResponseDto> UpdateUserAsync(Guid id, UserUpdateDto request)
        {

            var existingUser = await _userRepository.GetOnlyUserByIdAsync(id);
            if(existingUser == null)
            {
                throw new Exception($"User dengan ID {id} tidak ditemukan.");
            }

            // cek email
            var userWithSameEmail = await _userRepository.GetByEmailAsync(request.Email);
            if(userWithSameEmail != null && userWithSameEmail.Email != existingUser.Email)
            {
                throw new Exception($"Email '{request.Email}' sudah digunakan oleh user lain.");
            }

            // cek username
            var userWithSameUsername = await _userRepository.GetByUsernameAsync(request.Username);
            if(userWithSameUsername != null && userWithSameUsername.Username != existingUser.Username)
            {
                throw new Exception($"Username '{request.Username}' sudah digunakan oleh user lain");
            }

            // update
            var userToUpdate = new User
            {
                Id = id,
                Username = request.Username,
                Email = request.Email
            };

            await _userRepository.UpdateAsync(userToUpdate);

            // tarik ulang data segar untuk dikembalikan ke controller
            return (await GetUserByIdAsync(id))!;

        }

        // sinkronisasi role user
        public async Task SyncUserRolesAsync(Guid userId, SyncUserRolesRequestDto request)
        {

            // validasi user
            var user = await _userRepository.GetOnlyUserByIdAsync(userId);
            if(user == null)
            {
                throw new Exception($"User dengan ID {userId} tidak ditemukan");
            }

            await _userRepository.SyncRolesAsync(userId, request.RoleIds);
        
        }

    }
}
