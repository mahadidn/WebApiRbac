using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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

            // call generate jwt & refresh token
            var jwtToken = GenerateJwtToken(user, roles, permissions);
            var refreshTokenString = GenerateRefreshTokenString();

            // insert refresh token to database
            var refreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            await _userRepository.AddRefreshTokenAsync(refreshToken);

            // return the token as a string to the controller
            var expireMinutes = Convert.ToInt32(_configuration["Jwt:ExpireMinutes"] ?? "60");
            return new LoginResponseDto
            {
                AccessToken = jwtToken,
                TokenType = "Bearer",
                ExpiresIn = expireMinutes * 60, // change to second
                RefreshToken = refreshTokenString // Controller sangat butuh ini untuk membuat Cookie nanti!
            };

        }

        // refresh token async
        /*
            Ini adalah endpoint rahasia yang akan dipanggil oleh Frontend secara diam-diam.
            Di sini kita menerapkan teknik keamanan tingkat tinggi bernama Refresh Token Rotation. 
            Artinya, setiap kali user meminta JWT baru, Refresh Token yang lama langsung kita matikan (Revoke) dan 
            kita beri Refresh Token yang baru juga! Ini mencegah pencurian token ganda.
         */
        public async Task<LoginResponseDto> RefreshTokenAsync(string oldRefreshTokenString, string? ipAddress = null)
        {
            // find token in the database
            var existingToken = await _userRepository.GetRefreshTokenAsync(oldRefreshTokenString);

            // validation (check if its exist, or if its expired, or if its revoked)
            if (existingToken == null)
            {
                throw new Exception("Sesi telah berakhir atau tidak valid. Silakan login kembali.");
            }

            // alarm deteksi penggunaan ulang token (reuse detection)
            if (existingToken.IsRevoked)
            {
                // BAHAYA! Token ini sudah mati, tapi ada yang mencoba memakainya lagi!
                // Ini ciri khas token curian yang sedang dipakai attacker, atau user mengalami desinkronisasi.
                await _userRepository.RevokeAllRefreshTokensAsync(existingToken.UserId);
                throw new Exception("Suspicious activity detected! All sessions have been terminated for security reasons. Please log in again using your password.");
            }

            // validasi normal
            if (existingToken.IsExpired)
            {
                throw new Exception("Sesi telah berakhir. Silakan login kembali.");
            }

            // ambil data user beserta roles dan permission-nya
            // (existingToken.User tidak akan null karena kita pakai .Include() di Repository)
            var user = existingToken.User!;
            var roles = await _userRepository.GetUserRolesAsync(user.Id);
            var permissions = await _userRepository.GetUserPermissionsAsync(user.Id);

            // generate token
            var newJwtToken = GenerateJwtToken(user, roles, permissions);
            var newRefreshTokenString = GenerateRefreshTokenString();

            // membentuk rantai pelacak (token chain)
            // matikan token lama dan tulis token baru
            existingToken.Revoked = DateTime.UtcNow;
            existingToken.ReplacedByToken = newRefreshTokenString; // Ini adalah kunci pelacakannya!

            // refresh token rotation
            // matikan token lama agar tidak bisa dipakai lagi
            await _userRepository.UpdateRefreshTokenAsync(existingToken);

            // simpan token yg baru ke database
            var newRefreshToken = new RefreshToken
            {
                Token = newRefreshTokenString,
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(7),
                CreatedByIp = ipAddress
            };

            await _userRepository.AddRefreshTokenAsync(newRefreshToken);

            // kembalikan pasangan token baru ke controller
            var expireMinutes = Convert.ToInt32(_configuration["Jwt:ExpireMinutes"] ?? "60");
            return new LoginResponseDto
            {
                AccessToken = newJwtToken,
                TokenType = "Bearer",
                ExpiresIn = expireMinutes * 60,
                RefreshToken = newRefreshTokenString
            };

        }

        // buat untuk cetak access token (JWT)
        private string GenerateJwtToken(User user, IEnumerable<Role> roles, IEnumerable<string> permissions)
        {

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

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // pencetak refresh token
        private string GenerateRefreshTokenString()
        {
            // jangan pernah gunakan "new Random()" untuk keamanan
            // gunakan Cryptographically Secure Random Number Generator (CSRNG).
            var randomNumber = new Byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            // ubah byte acak menjadi string base64 agar aman dikirim via http
            return Convert.ToBase64String(randomNumber);
        }

        

    }
}
