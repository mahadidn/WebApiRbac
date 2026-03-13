namespace WebApiRbac.Domain.Entities
{
    public class UserRole
    {
        // nama properti
        public Guid UsersId { get; set; }
        public Guid RolesId { get; set; }

        // navigasi
        public User? User { get; set; }
        public Role? Role { get; set; }


    }
}
