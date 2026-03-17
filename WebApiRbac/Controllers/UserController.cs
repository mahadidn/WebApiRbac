using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApiRbac.Application.DTOs.Users;
using WebApiRbac.Application.Interfaces;

namespace WebApiRbac.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet] // api/user?pageNumber=1&pageSize=10
        public async Task<IActionResult> GetAllUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            // validasi keamanan
            if (pageNumber < 1) pageNumber = 1;
            // cegah ukuran per halaman yg tidak masuk akal
            if (pageSize < 1) pageSize = 10;
            // cegah penarikan data berlebihan (misal 10.000 data), kasih maxnya 100 data
            if(pageSize > 100) pageSize = 100;

            // terusin ke service
            var pagedResult = await _userService.GetAllUsersAsync(pageNumber, pageSize);

            // bungkus json
            return Ok(new
            {
                message = "Berhasil mengambil daftar user",
                data = pagedResult.Data,
                meta = new
                {
                    totalCount = pagedResult.TotalCount,
                    currentPage = pagedResult.CurrentPage,
                    pageSize = pagedResult.PageSize,
                    prevPage = pagedResult.PrevPage,
                    nextPage = pagedResult.NextPage,
                    totalPages = pagedResult.TotalPages
                }
            });
        }

        // get by id
        [HttpGet("{id}")] // GET api/user/{id}
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _userService.GetUserByIdAsync(id);

            if(user == null)
            {
                return NotFound(new { message = $"User dengan ID {id} tidak ditemukan" });
            }

            return Ok(new
            {
                message = "Berhasil mengambil detail user",
                data = user
            });
        }

        [HttpPut("{id}")] // PUT api/user/{id}
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateDto request)
        {
            try
            {
                // teruskan ke service
                var updatedUser = await _userService.UpdateUserAsync(id, request);

                // jika sukses, kembalikan 200
                return Ok(new
                {
                    message = "Berhasil mengubah data user",
                    data = updatedUser
                });
            }
            catch(Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/roles")] // PUT api/user/{id}/roles
        public async Task<IActionResult> SyncRoles(Guid id, [FromBody] SyncUserRolesRequestDto request)
        {
            try
            {
                await _userService.SyncUserRolesAsync(id, request);

                return Ok(new
                {
                    message = "Berhasil menyinkronkan role untuk user ini"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
