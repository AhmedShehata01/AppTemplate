namespace AppTemplate.BLL.Models
{
    public class UserSessionDTO
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpireAt { get; set; }
        public string ConnectionId { get; set; }
        public string UserName { get; set; }
        public List<string> Roles { get; set; } = new();
        public Dictionary<string, string> Claims { get; set; } = new();
        public DateTime CreatedAt { get; set; }

        public bool IsLoggedOut { get; set; }
        public bool ForceLoggedOut { get; set; }
        public DateTime LastActivityAt { get; set; }
    }
}
