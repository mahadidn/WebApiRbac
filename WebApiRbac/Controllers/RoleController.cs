using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApiRbac.Application.DTOs.Role;
using WebApiRbac.Application.Interfaces;

namespace WebApiRbac.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet] // GET /api/role
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleService.GetAllRolesAsync();

            // jika berhasil return
            return Ok(new
            {
                message = "Berhasil mengambil daftar role",
                data = roles
            });
        }

        [HttpGet("{id}")] // GET /api/role/{id}
        public async Task<IActionResult> GetRoleById(Guid id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);

            // jika datanya tidak ada, return 404
            if(role == null)
            {
                return NotFound(new
                {
                    message = $"Role dengan ID {id} tidak ditemukan"
                });
            }

            return Ok(new
            {
                message = "Berhasil mengambil detail role",
                data = role
            });
        }

        [HttpPost] // POST /api/role
        public async Task<IActionResult> CreateRole([FromBody] RoleRequestDto request)
        {
            try
            {
                var newRole = await _roleService.CreateRoleAsync(request);

                // jika berhasil membuat data, gunakan 201 created
                // CreatedAtAction akan memberitahu di mana URL data baru ini bisa dilihat (yaitu di fungsi GetRoleById) dan tidak akan melakukan query ke database
                return CreatedAtAction(
                    nameof(GetRoleById), // 1. "Tolong lihat rute dari fungsi GetRoleById" (Yaitu: GET api/role/{id})
                    new {id = newRole.Id}, // 2. "Ganti tulisan {id} di rute tersebut dengan ID Role yang baru saja kita buat di database"
                    new {message = "Berhasil membuat role baru", data = newRole } // 3. "Ini adalah bentuk JSON Response yang akan tampil 
                );
            }
            catch (Exception ex)
            {
                // jika melembar error (misal nama role sudah ada)
                // maka tangkap errornya, dan jadikan 400 bad request
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")] // PUT /api/role/{id}
        public async Task<IActionResult> UpdateRole(Guid id, [FromBody] RoleRequestDto request)
        {
            try
            {
                // update role ke service
                var updatedRole = await _roleService.UpdateRoleAsync(id, request);

                // jika sukses, kembalikan status 200 OK
                return Ok(new
                {
                    message = "Berhasil mengubah data role",
                    data = updatedRole
                });
            }
            catch(Exception ex)
            {
                // tangkap error
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")] // DELETE /api/role/{id}
        public async Task<IActionResult> DeleteRole(Guid id)
        {
            try
            {
                // eksekusi penghapusan lewat service
                await _roleService.DeleteRoleAsync(id);

                // return success
                return Ok(new
                {
                    message = "Berhasil menghapus role beserta seluruh relasi hak aksesnya"
                });
            }
            catch(Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("{id}/permissions")] // PUT /api/role/{id}/permissions
        public async Task<IActionResult> SyncPermissions(Guid id, [FromBody] SyncPermissionsRequestDto request)
        {
            try
            {
                // kirim ke service
                await _roleService.SyncRolePermissionAsync(id, request);

                // kembalikan response sukses
                return Ok(new { message = "Berhasil menyinkronkan hak akses untuk role ini" });
            }
            catch (Exception ex)
            {
                // tangkap error
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
