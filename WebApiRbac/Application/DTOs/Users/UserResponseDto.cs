namespace WebApiRbac.Application.DTOs.Users
{
    public class UserResponseDto
    {

        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // return a collection of texts (strings) with only the role names so that the JSON is clean.
        public List<UserRoleDto> Roles { get; set; } = new List<UserRoleDto>();

    }
}
