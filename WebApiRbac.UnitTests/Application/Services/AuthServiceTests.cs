using Microsoft.Extensions.Configuration;
using Moq;
using WebApiRbac.Application.DTOs.Auth;
using WebApiRbac.Application.Services;
using WebApiRbac.Domain.Interfaces;
using WebApiRbac.Domain.Entities;
using FluentAssertions;
using WebApiRbac.Application.DTOs.Users;
using Castle.Core.Logging;

namespace WebApiRbac.UnitTests.Application.Services
{
    public class AuthServiceTests
    {

        // siapkan mock
        private readonly Mock<IUserRepository> _userRepositoryMock;
        // 2. Gunakan Interface asli untuk konfigurasi
        private readonly IConfiguration _configuration;

        // siapkan service
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();

            // buat appsettings.json versi bohongan untuk configurationnya
            var inMemorySettings = new Dictionary<string, string?>
            {
                {"Jwt:Key", "KunciRahasiaSuperPanjangMinimal32Karakter!"},
                {"Jwt:ExpireMinutes", "60"},
                {"Jwt:Issuer", "TestIssuer"},
                {"Jwt:Audience", "TestAudience"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _authService = new AuthService(_userRepositoryMock.Object, _configuration);
        }

        // ===========================
        // TEST UNTUK: RegisterAsync
        // ==========================
        [Fact]
        public async Task RegisterAsync_EmailFound_MustThrowException()
        {
            // 1. arrange
            var request = new RegisterRequestDto
            {
                Username = "userbaru",
                Email = "userbarubentrok@test.com",
                Password = "Password!123"
            };

            // kalau service nyari email userbarubentrok@test.com, kembalikan objek user (artinya email sudah ada)
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(request.Email))
                .ReturnsAsync(new User { Id = Guid.CreateVersion7(), Email = request.Email });

            // 2. act
            Func<Task> act = async () => await _authService.RegisterAsync(request);

            // 3. assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("The email has been registered in the system.");

            // pastikan service tidak pernah menyimpan ke database
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);
                
        }
        [Fact]
        public async Task RegisterAsync_UsernameFound_MustThrowException()
        {
            // 1. arrange
            var newUsername = "mahadi_bentrok";
            var newEmail = "email_aman@test.com";

            // siapkan request
            var request = new RegisterRequestDto
            {
                Username = newUsername,
                Email = newEmail
            };

            // loloskan pengecekan email
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);

            // pas cek username, buat usernamse sudah ada
            _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync(request.Username))
                .ReturnsAsync(new User { Id = Guid.CreateVersion7(), Username = newUsername, Email = "mahadi@test.com" });

            // 2. act
            Func<Task> act = async () => await _authService.RegisterAsync(request);

            // 3. assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("The username has been registered in the system.");

            // pastikan service tidak menjalankan function untuk simpan ke database
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Never);

        }
        [Fact]
        public async Task RegisterAsync_DataValid_MustReturnSuccessAndHashPassword()
        {
            // 1. arrange
            var request = new RegisterRequestDto
            {
                Username = "mahadi_aman",
                Email = "mahadi_aman@test.com",
                Password = "RahasiaMahadi123$"
            };

            // loloskan pengecekan email dan usernae
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);
            _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync(request.Username))
                .ReturnsAsync((User?)null);

            // setup data untuk return dto
            var createdUserFromDb = new UserResponseDto
            {
                Username = request.Username,
                Email = request.Email,
                UpdatedAt = null,
                CreatedAt = DateTime.UtcNow,
                Roles = new List<UserRoleDto>()
            };

            // 2. act
            var result = await _authService.RegisterAsync(request);

            // 3. assert
            // apakah dtonya benar
            result.Should().NotBeNull();
            result.Username.Should().Be(request.Username);
            result.Email.Should().Be(request.Email);

            // tidak hanya mempertanyakan apakah memanggil fungsi AddAsync 1x
            // tetapi bongkar juga objek user, untuk cek passwordnya juga apakah di hash atau tidak
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.Is<User>(u => 
                u.Username == request.Username &&
                u.Email == request.Email &&
                // pastikan password di dalam objek tersebut bukan text asli, tapi yg udah di hash
                BCrypt.Net.BCrypt.Verify(request.Password, u.Password)
            )), Times.Once);

        }

        // ==========================
        // TEST UNTUK: LoginAsync
        // ==========================
        [Fact]
        public async Task LoginAsync_WrongUsernameOrEmail_MustThrowUnauthorized()
        {
            // 1. arrange
            var request = new LoginRequestDto
            {
                Identifier = "mahadi_notfound",
                Password = "MahadiRahasia123$"
            };

            // buat null saat pengecekan email/usernme
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(request.Identifier))
                .ReturnsAsync((User?)null);
            _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync(request.Identifier))
                .ReturnsAsync((User?)null);

            // 2. act
            Func<Task> act = async () => await _authService.LoginAsync(request);

            // 3. assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Username/Email or Password wrong");
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_MustThrowUnauthorized()
        {
            // 1. arrange
            var request = new LoginRequestDto
            {
                Identifier = "mahadi@test.com",
                Password = "PasswordyangSalah523$"
            };

            // buat user bohongan dengan password asli yg sudah di hash
            var realPassword = "PasswordBenar123@";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(realPassword);

            var userInDatabase = new User
            {
                Id = Guid.CreateVersion7(),
                Email = "mahadi@test.com",
                Password = hashedPassword
            };

            // kembalikan userInDatabase kalo nyari emailnya
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(request.Identifier))
                .ReturnsAsync(userInDatabase);

            // 2. act
            Func<Task> act = async () => await _authService.LoginAsync(request);

            // 3. assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Username/Email or Password wrong");

        }

        [Fact]
        public async Task LoginAsync_ValidData_MustReturnTokenAndStoreRefreshToken()
        {
            // 1. arrange
            var request = new LoginRequestDto
            {
                Identifier = "mahadi@test.com",
                Password = "PasswordBenar123$"
            };

            // siapkan user dengan password hash yg cocok dengan request
            var realUser = new User
            {
                Id = Guid.CreateVersion7(),
                Username = "mahadi_test",
                Email = "mahadi@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password)
            };


            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(request.Identifier))
                .ReturnsAsync(realUser);

            // setup tambahan untuk pembuatan token: tambahan role & permission
            _userRepositoryMock.Setup(repo => repo.GetUserRolesAsync(realUser.Id))
                .ReturnsAsync(new List<Role> { new Role { Name = "Admin" } });
            _userRepositoryMock.Setup(repo => repo.GetUserPermissionsAsync(realUser.Id))
                .ReturnsAsync(new List<string> { "users:read", "users:write" });

            // 2. act
            var ipAddress = "192.168.1.1";
            var result = await _authService.LoginAsync(request, ipAddress);

            // 3. assert
            // a. pengecekan DTO
            result.Should().NotBeNull();
            result.TokenType.Should().Be("Bearer");
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
            result.ExpiresIn.Should().Be(3600);

            // pastikan refresh token disimpan ke database
            _userRepositoryMock.Verify(repo => repo.AddRefreshTokenAsync(It.Is<RefreshToken>(rt =>
                rt.UserId == realUser.Id &&
                rt.CreatedByIp == ipAddress &&
                !string.IsNullOrEmpty(rt.Token)
            )), Times.Once);

        }

        // ========================
        // TEST UNTUK: RefreshTokenAsync
        // ========================
        [Fact]
        public async Task RefreshTokenAsync_TokenNotFound_MustThrowError()
        {
            // 1. arrange
            var fakeToken = "tokenNotFoundInDB";

            _userRepositoryMock.Setup(repo => repo.GetRefreshTokenAsync(fakeToken))
                .ReturnsAsync((RefreshToken?)null);

            // 2. act
            Func<Task> act = async () => await _authService.RefreshTokenAsync(fakeToken);

            // 3. assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Sesi telah berakhir atau tidak valid. Silakan login kembali.");
        }

        [Fact]
        public async Task RefreshTokenAsync_RevokedTokenBeingUsed_MustRevokeAllAndThrowError()
        {
            // 1. arrange
            var tokenStealing = "old_revoked_token";
            var victimUserId = Guid.CreateVersion7();

            var tokenFromDb = new RefreshToken
            {
                Token = tokenStealing,
                UserId = victimUserId,
                Revoked = DateTime.UtcNow.AddDays(-1), // token is revoked yesterday
            };

            _userRepositoryMock.Setup(repo => repo.GetRefreshTokenAsync(tokenStealing))
                .ReturnsAsync(tokenFromDb);

            // 2. act
            Func<Task> act = async () => await _authService.RefreshTokenAsync(tokenStealing);

            // 3. assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Suspicious activity detected! All sessions have been terminated for security reasons. Please log in again using your password.");

            // pastikan service memanggil revoke all sekali
            _userRepositoryMock.Verify(repo => repo.RevokeAllRefreshTokensAsync(victimUserId), Times.Once);
        }

        [Fact]
        public async Task RefreshTokenAsync_TokenExpired_MustThrowError()
        {
            // 1. arrange
            var tokenExpired = "token_has_expired";
            var tokenFromDb = new RefreshToken
            {
                Token = tokenExpired,
                Revoked = null,
                Expires = DateTime.UtcNow.AddDays(-1) // non active
            };

            // 2. act
            Func<Task> act = async () => await _authService.RefreshTokenAsync(tokenExpired);

            // 3. assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Sesi telah berakhir atau tidak valid. Silakan login kembali.");
        }

        [Fact]
        public async Task RefreshTokenAsync_ValidToken_MustTokenRotationAndReturnNewRefreshToken()
        {
            // 1. arrange
            var oldToken = "valid_old_token";
            var ipAddress = "192.168.100.5";
            var realUser = new User
            {
                Id = Guid.CreateVersion7(),
                Username = "mahadi",
                Email = "mahadi@test.com"
            };

            var tokenFromDb = new RefreshToken
            {
                Token = oldToken,
                Revoked = null,
                Expires = DateTime.UtcNow.AddDays(2), // still alive for 2 days
                User = realUser  
            };

            _userRepositoryMock.Setup(repo => repo.GetRefreshTokenAsync(oldToken))
                .ReturnsAsync(tokenFromDb);

            // setup role & permission for new JTW
            _userRepositoryMock.Setup(repo => repo.GetUserRolesAsync(realUser.Id))
                .ReturnsAsync(new List<Role>());
            _userRepositoryMock.Setup(repo => repo.GetUserPermissionsAsync(realUser.Id))
                .ReturnsAsync(new List<string>());

            // 2. act
            var result = await _authService.RefreshTokenAsync(oldToken, ipAddress);

            // 3. assert
            result.Should().NotBeNull();
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBe(oldToken);

            // pastikan mematikan token lama
            _userRepositoryMock.Verify(repo => repo.UpdateRefreshTokenAsync(It.Is<RefreshToken>(rt =>
                rt.Token == oldToken &&
                rt.Revoked != null && // harus sudah dimatikan
                rt.ReplacedByToken == result.RefreshToken  // rantai pelacaknya harus tersambung

            )), Times.Once);

            // pastikan service menyimpan token baru ke database
            _userRepositoryMock.Verify(repo => repo.AddRefreshTokenAsync(It.Is<RefreshToken>(rt =>
                rt.Token == result.RefreshToken &&
                rt.UserId == realUser.Id &&
                rt.CreatedByIp == ipAddress
            )), Times.Once);

        }


        // ========================
        // TEST UNTUK: LogoutAsync
        // ========================
        [Fact]
        public async Task LogoutAsync_TokenNotFound_DoNothing()
        {
            // 1. arrange
            var fakeToken = "tokenfakse";
            _userRepositoryMock.Setup(repo => repo.GetRefreshTokenAsync(fakeToken))
                .ReturnsAsync((RefreshToken?)null);

            // 2. act
            // tidak perlu dibungkus Func<Task> karena tidak memprediksi adanya error
            await _authService.LogoutAsync(fakeToken);

            // 3. assert
            // pastikan service ga ngelakukan update ke database
            _userRepositoryMock.Verify(repo => repo.UpdateRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_TokenExpired_DoNothing()
        {
            // 1. arrange
            var tokenExpired = "token_expired";
            var tokenFromDb = new RefreshToken
            {
                Token = tokenExpired,
                Expires = DateTime.UtcNow.AddDays(-1), // already expired
                Revoked = null,
            };

            _userRepositoryMock.Setup(repo => repo.GetRefreshTokenAsync(tokenExpired))
                .ReturnsAsync(tokenFromDb);

            // 2. act
            await _authService.LogoutAsync(tokenExpired);

            // 3. assert
            // pastikan service melihat token mati, lalu membataklan update
            _userRepositoryMock.Verify(repo => repo.UpdateRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_TokenActive_MustRevokeAndUpdateDatabase()
        {
            // 1. arrange
            var validToken = "token_valid_active";
            var tokenFromDb = new RefreshToken
            {
                Token = validToken,
                Expires = DateTime.UtcNow.AddDays(6), // masih aktif 6 hari lagi
                Revoked = null
            };

            _userRepositoryMock.Setup(repo => repo.GetRefreshTokenAsync(validToken))
                .ReturnsAsync(tokenFromDb);

            // 2. act
            await _authService.LogoutAsync(validToken);

            // 3. assert
            // pastikan service memanggil repository untuk update token ke database
            // dan pastikan service sudah mengisi tanggal revoked 
            _userRepositoryMock.Verify(repo => repo.UpdateRefreshTokenAsync(It.Is<RefreshToken>(rt =>
                rt.Token == validToken &&
                rt.Revoked != null // ini membuktikan token benar-benar dimatikan
            )), Times.Once);
        }

    }
}
