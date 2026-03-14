namespace WebApiRbac.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.CreateVersion7();
        public string Token { get; set; } = string.Empty;
        public DateTime Expires { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public string? CreatedByIp { get; set; }
        public DateTime? Revoked { get; set; }

        // relasi ke user
        public Guid UserId { get; set; }
        public User? User { get; set; }

        // computed properties (Logika pintar yg tidak disimpan di DB)
        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsRevoked => Revoked != null;
        public bool IsActive => !IsRevoked && !IsExpired;


    }
}
