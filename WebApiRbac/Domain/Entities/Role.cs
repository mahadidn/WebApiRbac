namespace WebApiRbac.Domain.Entities
{
    public class Role
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty; // to prevent nullable
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // many to many relation: one role can be owned by multiple users.
        public ICollection<User> Users { get; set; } = new List<User>();

        // many to many relation: one role can be owned by multiple permissions
        public ICollection<Permission> Permissions { get; set; } = new List<Permission>();


    }
}
