namespace AppTemplate.BLL.Models.Auth
{
    public class ExternalUserInfoDto
    {
        public string Provider { get; set; } = default!;
        public string ProviderUserId { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string FullName { get; set; } = default!;
    }
}
