using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
        private readonly IConfiguration _configuration;

        // second layer: dependency injection
        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }

        // third layer: main business logic
        // register user
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

        // login
        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // find user, with username or email
            var user = await _userRepository.GetByEmailAsync(request.Identifier)
                    ?? await _userRepository.GetByUsernameAsync(request.Identifier);

            // if user not found, throw error
            if (user == null)
            {
                throw new UnauthorizedAccessException("Username/Email or Password wrong");
            }

            // verify with bcrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Username/Email or Password wrong");
            }

            // retrieve role and permission data
            var roles = await _userRepository.GetUserRolesAsync(user.Id);
            var permissions = await _userRepository.GetUserPermissionsAsync(user.Id);

            // prepare the “payload” (claims) to be included in the JWT
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // subject id user
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // unique id token
                new Claim(ClaimTypes.NameIdentifier, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // include all roles in the token payload
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.Name));
            }

            // include all permissons in the token payload
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("Permission", permission));
            }

            // retrieve secret key from usersecrets
            var keyString = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(keyString) || keyString.Length < 32)
            {
                throw new Exception("The JWT key configuration is invalid or contains fewer than 32 characters");
            }

            // buat segel kriptografi (signature)
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // rakit tiket jwt
            var expireMinutes = Convert.ToInt32(_configuration["Jwt:ExpireMinutes"] ?? "60");
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds
            );

            // return the token as a string to the controller
            return new LoginResponseDto
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
                TokenType = "Bearer",
                ExpiresIn = expireMinutes * 60 // change to second
            };

        }



    }
}
