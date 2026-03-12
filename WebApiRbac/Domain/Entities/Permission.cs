namespace WebApiRbac.Domain.Entities
{
    public class Permission
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // many to many relation (one permission can be owned by multiple roles)
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}
