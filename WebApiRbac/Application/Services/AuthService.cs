using WebApiRbac.Application.DTOs.Auth;
using WebApiRbac.Application.DTOs.Users;
using WebApiRbac.Application.Interfaces;
using WebApiRbac.Domain.Entities;
using WebApiRbac.Domain.Interfaces;

namespace WebApiRbac.Application.Services
{

    // first layer: implement interface
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        // second layer: dependency injection
        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // third layer: main business logic
        public async Task<UserResponseDto> RegisterAsync(RegisterRequestDto request)
        {

            // 1. business validation: check the email is already in use
            var existingEmail = await _userRepository.GetByEmailAsync(request.Email);
            if (existingEmail != null)
            {
                // custom exception
                throw new Exception("The email has been registered in the system.");
            }

            // check the user is already in use
            var existingUsername = await _userRepository.GetByUsernameAsync(request.Username);
            if (existingUsername != null)
            {
                // custom exception
                throw new Exception("The username has been registered in the system.");
            }

            // hash password with BCrypt
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var newUser = new User
            {
                Username = request.Username,
                Email = request.Email,
                Password = hashedPassword
            };

            // store in database
            await _userRepository.AddAsync(newUser);

            // mapping, change to DTO response
            return new UserResponseDto
            {
                Id = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                CreatedAt = newUser.CreatedAt,
                Roles = new List<string>()
            };


        }


    }
}
