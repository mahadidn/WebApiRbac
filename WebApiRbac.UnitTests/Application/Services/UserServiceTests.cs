using Moq;
using WebApiRbac.Application.DTOs.Users;
using WebApiRbac.Application.Services;
using WebApiRbac.Domain.Interfaces;
using WebApiRbac.Domain.Entities;
using FluentAssertions;

namespace WebApiRbac.UnitTests.Application.Services
{
    public class UserServiceTests
    {

        // siapkan "remote control" untuk mengatur gudang palsu (Mock)
        private readonly Mock<IUserRepository> _userRepositoryMock;

        // siapkan service asli yg mau di uji
        private readonly UserService _userService;

        // constructor ini akan otomatis dijalankan sebelum setiap skenario test dimulai
        public UserServiceTests()
        {
            // bikin gudang palsu baru (mock)
            _userRepositoryMock = new Mock<IUserRepository>();

            // panggil service, tapi jangan kasih gudang asli (database)
            // kasih gudang palsu menggunakan .Object
            _userService = new UserService(_userRepositoryMock.Object);
        }

        // ==================================
        // TEST UNTUK: GetAllUsersAsync
        // ===============================
        [Fact]
        public async Task GetAllUsersAsync_ExistsData_MustReturnPagedResponse()
        {
            // 1. arrange
            int pageNumber = 1;
            int pageSize = 10;
            int totalDataInDB = 50;

            var user1 = new User
            {
                Id = Guid.CreateVersion7(),
                Username = "first_user",
                Email = "first@test.com",
                Roles = new List<Role>
                {
                    new Role {Name = "Admin"}
                }
            };

            var user2 = new User
            {
                Id = Guid.CreateVersion7(),
                Username = "second_user",
                Email = "second@test.com",
                Roles = new List<Role>
                {
                    new Role {Name = "Staff"},
                    new Role {Name = "Manager"}
                }
            };

            var mockUserList = new List<User> { user1, user2 };

            // kalau minta data halaman 1 isi 10, kasih list ini dan bilang totalnya 50
            _userRepositoryMock.Setup(repo => repo.GetAllAsync(pageNumber, pageSize))
                .ReturnsAsync((mockUserList, totalDataInDB));

            // 2. act
            var result = await _userService.GetAllUsersAsync(pageNumber, pageSize);

            // 3. assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(50);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(10);

            // cek isi datanya
            var dataList = result.Data.ToList();
            dataList.Should().HaveCount(2);

            var firstUser = dataList[0];
            firstUser.Username.Should().Be("first_user");
            firstUser.Roles.Should().ContainSingle().Which.Should().Be("Admin");

            var secondUser = dataList[1];
            secondUser.Roles.Should().HaveCount(2);
            secondUser.Roles.Should().Contain("Staff");
            secondUser.Roles.Should().Contain("Manager");
        }

        [Fact]
        public async Task GetAllUsersAsync_EmptyData_MustReturnEmpty()
        {
            // 1. arrange
            int pageNumber = 1;
            int pageSize = 10;

            // list kosong & total count 0
            var emptyList = new List<User>();

            _userRepositoryMock.Setup(repo => repo.GetAllAsync(pageNumber, pageSize))
                .ReturnsAsync((emptyList, 0));

            // 2. act
            var result = await _userService.GetAllUsersAsync(pageNumber, pageSize);

            // 3. assert
            result.Should().NotBeNull();
            result.TotalCount.Should().Be(0);
            result.Data.Should().BeEmpty();

        }

        // ===============================
        // TEST FOR: GetUserByIdAsync
        // =============================
        [Fact]
        public async Task GetUserByIdAsync_UserNotFound_MustReturnNull()
        {
            // 1. arrange
            var searchId = Guid.CreateVersion7();

            // kalau cari id ini, bilang aja datanya ga ada (null)
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(searchId))
                .ReturnsAsync((User?)null);

            // 2. act
            var result = await _userService.GetUserByIdAsync(searchId);

            // 3. assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByIdAsync_UserFound_MustReturnDtoWithRoles()
        {
            // 1. arrange
            var id = Guid.CreateVersion7();

            // siapkan data dari database yg punya 2 role
            var userFromDatabase = new User
            {
                Id = id,
                Username = "mahadi_dn",
                Email = "mahadi_get@test.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Roles = new List<Role>
                {
                    new Role{ Id = Guid.CreateVersion7(), Name = "Manager" },
                    new Role{ Id = Guid.CreateVersion7(), Name = "HR" }
                }
            };

            // kembalikan data kompleks, kalau cari id tersebut
            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(id))
                .ReturnsAsync(userFromDatabase);

            // 2. act
            var result = await _userService.GetUserByIdAsync(id);

            // 3. assert
            result.Should().NotBeNull();

            // pastikan datanya termapping dengan benar
            result!.Id.Should().Be(id);
            result.Username.Should().Be("mahadi_dn");

            // pastikan list roles juga ikut termapping 
            result.Roles.Should().NotBeNull();
            result.Roles.Should().HaveCount(2); // harus persis 2 role

            // cek role pertama
            result.Roles[0].Name.Should().Be("Manager");
            result.Roles[1].Name.Should().Be("HR");

        }

        // ===============================
        // TEST FOR: UpdateUserAsync
        // =============================
        [Fact] // atribut ini memberi tahu xUnit bahwa fungsi ini hanya sebuah Test
        public async Task UpdateUserAsync_EmailIsUsed_MustThrowError()
        {
            // 1. arrange (persiapan panggung)
            var userIdToBeEdited = Guid.CreateVersion7();
            var newEmail = "mahadi@test.com";

            // siapkan request dari front end
            var request = new UserUpdateDto
            {
                Username = "mahadi",
                Email = newEmail
            };

            // setting gudang palsunya disini:
            // cek ke gudang palsu, kalau manggil fungsi GetOnlyUserByIdAsync untuk ngecek user ini ada atau nggak,
            // tolong kembalikan data user bohongan ini (artinya usernya ditemukan jadi bentrok)
            _userRepositoryMock.Setup(repo => repo.GetOnlyUserByIdAsync(userIdToBeEdited))
                .ReturnsAsync(new User { Id = userIdToBeEdited });

            // loloskan pengecekan username
            _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync(newEmail))
                .ReturnsAsync((User?)null);

            // gudang palsu, kalau si fe manggil fungsi GetByEmailAsync nyari 'mahadi@test.com'
            // tolong kembalikan data user lain (ID-nya beda) yg udah pakai email ini
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(newEmail))
                .ReturnsAsync(new User { Id = Guid.CreateVersion7(), Email = newEmail });

            // 2. act (eksekusi)
            // rekam aksi service saat disuruh melakukan update
            // pakai Func<Task> karna memprediksi ini akan error
            Func<Task> act = async () => await _userService.UpdateUserAsync(userIdToBeEdited, request);

            // assert (pembuktian dengan FluentAssertions
            // kita buktikan bahwa aksi diatas harus melempar error (Exception), dan harus mengandung kata sudah digunakan
            await act.Should().ThrowAsync<Exception>()
                .WithMessage($"Email '{newEmail}' sudah digunakan oleh user lain.");
        }

        [Fact]
        public async Task UpdateUserAsync_UsernameIsUsed_MustThrowError()
        {
            // 1. arrange (persiapan panggung)
            var userIdToBeEdited = Guid.CreateVersion7();
            var newUsername = "mahadi_bentrok";
            var newEmail = "email_aman@test.com";

            // siapkan request dari front end
            var request = new UserUpdateDto
            {
                Username = newUsername,
                Email = newEmail
            };


            // loloskan pengecekan ID (akan return user tidak null, jadi aman dari logic di UpdateUserAsync (bagian 2 act)
            _userRepositoryMock.Setup(repo => repo.GetOnlyUserByIdAsync(userIdToBeEdited))
                .ReturnsAsync(new User { Id = userIdToBeEdited });

            // loloskan pengecekan email
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(newEmail))
                .ReturnsAsync((User?)null);

            // bikin bentrok di username
            // saat ngecek Username, kembalikan user lain (ID beda) yang pakai username tersebut
            _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync(newUsername))
                .ReturnsAsync(new User { Id = Guid.CreateVersion7(), Username = newUsername });

            // 2. act (eksekusi)
            Func<Task> act = async () => await _userService.UpdateUserAsync(userIdToBeEdited, request);

            // 3. assert (pembuktian dengan FluentAssertions)
            await act.Should().ThrowAsync<Exception>()
                .WithMessage($"Username '{newUsername}' sudah digunakan oleh user lain");
        }

        [Fact]
        public async Task UpdateUserAsync_DataValid_MustReturnSuccessAndDto()
        {
            // 1. arrange
            var userId = Guid.CreateVersion7();

            var request = new UserUpdateDto
            {
                Username = "mahadi_updated",
                Email = "mahadi_baru@test.com"
            };

            // data lama untuk dicek id nya
            var existingUser = new User
            {
                Id = userId,
                Username = "mahadi_lama",
                Email = "mahadi_lama@test.com"
            };

            // loloskan pengecekan apakah user ada?
            _userRepositoryMock.Setup(repo => repo.GetOnlyUserByIdAsync(userId))
                .ReturnsAsync(existingUser);

            // loloskan pengecekan email (tidak ada yg pakai -> return null)
            _userRepositoryMock.Setup(repo => repo.GetByEmailAsync(request.Email))
                .ReturnsAsync((User?)null);

            // loloskan pengecekan username (aman, tidak ada yg pakai -> return null)
            _userRepositoryMock.Setup(repo => repo.GetByUsernameAsync(request.Username))
                .ReturnsAsync((User?)null);

            // setup data segar untuk return dto diakhir fungsi
            var updatedUserFromDb = new User
            {
                Id = userId,
                Username = request.Username,
                Email = request.Email,
                Roles = new List<Role>() // kasih list kosong agar mapping dto tidak error
            };

            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(updatedUserFromDb);

            // 2. act
            // tiadk pakai Func<Task> karna yakin ini akan sukses & tidak error throw exception seperti sebelumnya
            var result = await _userService.UpdateUserAsync(userId, request);

            // 3. assert
            // apakah dto-nya benar
            result.Should().NotBeNull();
            result.Username.Should().Be(request.Username);
            result.Email.Should().Be(request.Email);
            result.Id.Should().Be(userId);

            // apakah benar-benar memanggil fungsi UpdateAsync
            // dan apakah dia memanggilnya tepat satu kali (Times.Once)?
            _userRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<User>()), Times.Once);

        }

        // =========================
        // TEST UNTUK: SyncUserRolesAsync
        // =========================
        [Fact]
        public async Task SyncUserRolesAsync_UserNotFound_MustReturnError()
        {
            // 1. arrange
            var userIdRandom = Guid.CreateVersion7();
            var request = new SyncUserRolesRequestDto
            {
                RoleIds = new List<Guid> { Guid.CreateVersion7() }
            };

            // kalau ditanaya ID ini, bilang aja null
            _userRepositoryMock.Setup(repo => repo.GetOnlyUserByIdAsync(userIdRandom))
                .ReturnsAsync((User?)null);

            // 2. act
            Func<Task> act = async () => await _userService.SyncUserRolesAsync(userIdRandom, request);

            // 3. assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage($"User dengan ID {userIdRandom} tidak ditemukan");

            // pastikan service tidak pernah memanggil fungsi SyncRolesAsync, karna sudah di exception, jadi SyncRolesAsync di repo ga dijalanin lagi
            // Times.Never() memastikan bahwa eksekusi benar-benar terhenti di Exception.
            _userRepositoryMock.Verify(repo => repo.SyncRolesAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>()), Times.Never);

        }

        [Fact]
        public async Task SyncUserRolesAsync_FoundUser_MustCallSyncFunc()
        {
            // 1. arrange
            var userId = Guid.CreateVersion7();
            var request = new SyncUserRolesRequestDto
            {
                RoleIds = new List<Guid>
                {
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7()
                }
            };

            // kalau service cek eksitensi user ini, bilang ada
            _userRepositoryMock.Setup(repo => repo.GetOnlyUserByIdAsync(userId))
                .ReturnsAsync(new User { Id = userId });

            // tidak perlu nge-setup fungsi SyncRolesAsync karena dia cuma void (Task kosong).
            // mock akan otomatis pura-pura mengerjakannya tanpa error.

            // 2. act
            await _userService.SyncUserRolesAsync(userId, request);

            // 3. assert
            // Karena tidak ada DTO yang di-return, satu-satunya cara membuktikan kesuksesan 
            // adalah dengan mengecek rekaman CCTV Moq

            // Apakah service benar-benar memanggil fungsi SyncRolesAsync dengan userId?
            // dan list RoleIds dari request, dan memanggilnya hanya satu kali?
            _userRepositoryMock.Verify(repo => repo.SyncRolesAsync(userId, request.RoleIds), Times.Once);
        
        }

    }
}
