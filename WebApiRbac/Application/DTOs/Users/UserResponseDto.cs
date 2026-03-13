namespace WebApiRbac.Application.DTOs.Users
{
    public class UserResponseDto
    {

        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // return a collection of texts (strings) with only the role names so that the JSON is clean.
        public IEnumerable<string> Roles { get; set; } = new List<string>();

    }
}
