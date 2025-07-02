namespace Kindergarten.BLL.Models.Auth
{
    public class AuthResponseDto
    {
        public bool IsAuthenticated { get; set; }
        public string? Token { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public DateTime? ExpireOn { get; set; }
        public string? Provider { get; set; } // ex: Google, Facebook
    }
}
