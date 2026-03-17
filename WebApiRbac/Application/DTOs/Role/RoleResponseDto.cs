namespace WebApiRbac.Application.DTOs.Role
{
    public class RoleResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<string> Permissions { get; set; } = new List<string>();

    }
}
