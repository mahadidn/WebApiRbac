namespace WebApiRbac.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // many to many relation: one user can be owned by multiple roles
        public ICollection<Role> Roles { get; set; } = new List<Role>();

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    }
}
